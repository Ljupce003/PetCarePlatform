using System.Diagnostics;
using System.Text.Json;
using Confluent.Kafka;
using Microsoft.EntityFrameworkCore;
using Shared.AppointmentEvents;
using Shared.Messaging;
using TreatmentAndNotificationService.Domain.Entities;
using TreatmentAndNotificationService.Domain.Enums;
using Xunit;

namespace TreatmentAndNotificationService.Api.IntegrationTests;

/// <summary>Exercises the real Kafka broker, hosted consumer, application service, and PostgreSQL repository.</summary>
[Collection(KafkaPostgreSqlCollection.Name)]
public sealed class AppointmentEventConsumerIntegrationTests : IAsyncLifetime
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private static readonly Guid AppointmentId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid PetId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private static readonly Guid OwnerId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
    private static readonly Guid VeterinarianId = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");

    private readonly KafkaPostgreSqlFixture _infrastructure;
    private readonly string _topic = $"petcare.appointments.tests.{Guid.NewGuid():N}";
    private readonly string _deadLetterTopic = $"petcare.appointments.tests.dlq.{Guid.NewGuid():N}";
    private TreatmentKafkaFactory _factory = null!;
    private HttpClient _client = null!;

    public AppointmentEventConsumerIntegrationTests(KafkaPostgreSqlFixture infrastructure) =>
        _infrastructure = infrastructure;

    public async Task InitializeAsync()
    {
        _factory = new TreatmentKafkaFactory(
            _infrastructure.ConnectionString,
            _infrastructure.KafkaBootstrapAddress,
            _topic,
            _deadLetterTopic);
        _client = _factory.CreateClient();
        await _factory.ResetDatabaseAsync();
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        await _factory.DisposeAsync();
    }

    [Fact]
    public async Task ScheduledDuplicateAndRescheduledEvents_CreateOneNotificationPerUniqueEvent()
    {
        var scheduledEventId = Guid.NewGuid();
        var rescheduledEventId = Guid.NewGuid();
        var scheduled = new AppointmentScheduledEvent(
            scheduledEventId,
            AppointmentId,
            PetId,
            OwnerId,
            VeterinarianId,
            DateTimeOffset.UtcNow.AddDays(3),
            DateTimeOffset.UtcNow.AddDays(3).AddMinutes(30),
            "Routine examination");
        var rescheduled = new AppointmentRescheduledEvent(
            rescheduledEventId,
            AppointmentId,
            PetId,
            OwnerId,
            VeterinarianId,
            DateTimeOffset.UtcNow.AddDays(4),
            DateTimeOffset.UtcNow.AddDays(4).AddMinutes(30));

        await PublishAsync(scheduledEventId, scheduled);
        await PublishAsync(scheduledEventId, scheduled); // Kafka redelivery/replay of the same occurrence.
        await PublishAsync(rescheduledEventId, rescheduled);

        var notifications = await WaitForNotificationCountAsync(4);

        Assert.Equal(4, notifications.Count);
        Assert.Equal(2, notifications.Count(item => item.Type == NotificationType.AppointmentScheduled));
        Assert.Equal(2, notifications.Count(item => item.Type == NotificationType.AppointmentRescheduled));
        Assert.Equal(4, notifications.Select(item => item.SourceEventId.Value).Distinct().Count());
    }

    [Fact]
    public async Task MalformedEvent_IsDeadLetteredAndDoesNotBlockTheNextValidEvent()
    {
        const string malformedValue = "this-is-not-an-integration-event-envelope";
        await PublishRawAsync(Guid.NewGuid().ToString(), malformedValue);

        var cancelledEventId = Guid.NewGuid();
        await PublishAsync(cancelledEventId, new AppointmentCancelledEvent(
            cancelledEventId,
            AppointmentId,
            PetId,
            OwnerId,
            VeterinarianId,
            DateTimeOffset.UtcNow,
            "Owner request"));

        var notifications = await WaitForNotificationCountAsync(2);
        Assert.All(notifications, item => Assert.Equal(NotificationType.AppointmentCancelled, item.Type));

        using var deadLetterConsumer = new ConsumerBuilder<string, string>(new ConsumerConfig
        {
            BootstrapServers = _infrastructure.KafkaBootstrapAddress,
            GroupId = $"treatment-dlq-assertion-{Guid.NewGuid():N}",
            AutoOffsetReset = AutoOffsetReset.Earliest
        }).Build();
        deadLetterConsumer.Subscribe(_deadLetterTopic);

        var deadLetter = deadLetterConsumer.Consume(TimeSpan.FromSeconds(20));
        Assert.NotNull(deadLetter);

        using var document = JsonDocument.Parse(deadLetter.Message.Value);
        Assert.Equal(_topic, document.RootElement.GetProperty("originalTopic").GetString());
        Assert.Equal(malformedValue, document.RootElement.GetProperty("value").GetString());
        Assert.Equal(2, document.RootElement.GetProperty("attempts").GetInt32());
    }

    private async Task PublishAsync<T>(Guid eventId, T message)
    {
        var envelope = new IntegrationEventEnvelope(
            typeof(T).Name,
            JsonSerializer.Serialize(message, JsonOptions),
            DateTimeOffset.UtcNow,
            eventId);

        await PublishRawAsync(eventId.ToString(), JsonSerializer.Serialize(envelope, JsonOptions));
    }

    private async Task PublishRawAsync(string key, string value)
    {
        using var producer = new ProducerBuilder<string, string>(new ProducerConfig
        {
            BootstrapServers = _infrastructure.KafkaBootstrapAddress,
            Acks = Acks.All
        }).Build();

        await producer.ProduceAsync(_topic, new Message<string, string> { Key = key, Value = value });
    }

    private async Task<List<Notification>> WaitForNotificationCountAsync(int expected)
    {
        var timeout = Stopwatch.StartNew();

        while (timeout.Elapsed < TimeSpan.FromSeconds(20))
        {
            var notifications = await _factory.WithDbContextAsync(db => db.Notifications
                .AsNoTracking()
                .OrderBy(item => item.CreatedAtUtc)
                .ToListAsync());

            if (notifications.Count == expected)
                return notifications;

            await Task.Delay(100);
        }

        var actual = await _factory.WithDbContextAsync(db => db.Notifications.CountAsync());
        throw new TimeoutException($"Expected {expected} notifications, but observed {actual}.");
    }
}
