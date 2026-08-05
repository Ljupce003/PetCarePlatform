using System.Text.Json;
using Confluent.Kafka;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Shared.Messaging;

public sealed class KafkaOptions
{
    public string BootstrapServers { get; set; } = "localhost:29092";
    public string ClientId { get; set; } = "petcare";
}

/// <summary>
/// Wraps every published event with what a consumer needs to route and correlate it, without
/// forcing consumers to reflect on the concrete .NET type: the event's name, its JSON payload,
/// when it happened, and a correlation id (defaults to the event's own id, see
/// <see cref="KafkaIntegrationEventPublisher.PublishAsync{T}"/>).
/// </summary>
public sealed record IntegrationEventEnvelope(
    string EventType,
    string Payload,
    DateTimeOffset OccurredAtUtc,
    Guid CorrelationId);

public interface IIntegrationEventPublisher
{
    Task PublishAsync<T>(string topic, T message, CancellationToken cancellationToken = default);
}

/// <summary>
/// Publishes integration events to Kafka. Configured for at-least-once, in-order delivery per key
/// (<c>Acks.All</c> + <c>EnableIdempotence</c>) so a retried publish can't create duplicate or
/// reordered records for the same event — the closest a fire-and-forget producer gets to
/// idempotent without the consumer also deduplicating by <see cref="IntegrationEventEnvelope.CorrelationId"/>.
/// </summary>
public sealed class KafkaIntegrationEventPublisher : IIntegrationEventPublisher, IDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly IProducer<string, string> _producer;

    public KafkaIntegrationEventPublisher(IOptions<KafkaOptions> options)
    {
        var config = new ProducerConfig
        {
            BootstrapServers = options.Value.BootstrapServers,
            ClientId = options.Value.ClientId,
            Acks = Acks.All,
            EnableIdempotence = true
        };
        _producer = new ProducerBuilder<string, string>(config).Build();
    }

    public async Task PublishAsync<T>(string topic, T message, CancellationToken cancellationToken = default)
    {
        // Every event record in Shared.AppointmentEvents starts with a Guid EventId — reused as
        // both the Kafka message key (so all events for the same occurrence land on the same
        // partition, in order) and the envelope's correlation id, without this publisher needing
        // to know about each event type individually.
        var eventIdValue = message?.GetType().GetProperty("EventId")?.GetValue(message);
        var correlationId = eventIdValue is Guid eventId ? eventId : Guid.NewGuid();

        var envelope = new IntegrationEventEnvelope(
            typeof(T).Name,
            JsonSerializer.Serialize(message, JsonOptions),
            DateTimeOffset.UtcNow,
            correlationId);

        await _producer.ProduceAsync(topic, new Message<string, string>
        {
            Key = correlationId.ToString(),
            Value = JsonSerializer.Serialize(envelope, JsonOptions)
        }, cancellationToken);
    }

    public void Dispose()
    {
        // Blocks briefly so in-flight events aren't silently dropped on shutdown.
        _producer.Flush(TimeSpan.FromSeconds(5));
        _producer.Dispose();
    }
}

public static class KafkaDependencyInjection
{
    public static IServiceCollection AddPetCareKafka(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<KafkaOptions>(configuration.GetSection("Kafka"));
        services.AddSingleton<IIntegrationEventPublisher, KafkaIntegrationEventPublisher>();
        return services;
    }
}
