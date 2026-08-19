using System.Text.Json;
using Confluent.Kafka;
using Microsoft.Extensions.Options;
using Shared.AppointmentEvents;
using Shared.Messaging;
using TreatmentAndNotificationService.Application.Services;

namespace TreatmentAndNotificationService.Infrastructure.Messaging;

public sealed class AppointmentEventConsumer : BackgroundService
{
    private readonly IOptions<KafkaConsumerOptions> _options;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<AppointmentEventConsumer> _logger;

    private static readonly JsonSerializerOptions JsonOptions =
        new(JsonSerializerDefaults.Web);

    // ReSharper disable once ConvertToPrimaryConstructor
    public AppointmentEventConsumer(
        IOptions<KafkaConsumerOptions> options,
        IServiceScopeFactory scopeFactory,
        ILogger<AppointmentEventConsumer> logger)
    {
        _options = options;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ConsumeUntilConnectionFailureAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                _logger.LogError(exception,
                    "Kafka consumer stopped unexpectedly. It will reconnect after {RetryDelayMilliseconds} ms.",
                    _options.Value.RetryDelayMilliseconds);
                await DelayBeforeRetryAsync(stoppingToken);
            }
        }
    }

    private async Task ConsumeUntilConnectionFailureAsync(CancellationToken stoppingToken)
    {
        var consumerConfig = new ConsumerConfig
        {
            BootstrapServers = _options.Value.BootstrapServers,
            GroupId = _options.Value.GroupId,
            EnableAutoCommit = false,
            AutoOffsetReset = AutoOffsetReset.Earliest
        };

        var producerConfig = new ProducerConfig
        {
            BootstrapServers = _options.Value.BootstrapServers,
            ClientId = $"{_options.Value.GroupId}-dead-letter-producer",
            Acks = Acks.All,
            EnableIdempotence = true,
            MessageTimeoutMs = 10_000
        };

        using var consumer = new ConsumerBuilder<string, string>(consumerConfig).Build();
        using var deadLetterProducer = new ProducerBuilder<string, string>(producerConfig).Build();

        consumer.Subscribe(_options.Value.Topic);

        _logger.LogInformation(
            "Kafka consumer group {GroupId} subscribed to {Topic}.",
            _options.Value.GroupId,
            _options.Value.Topic);

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                ConsumeResult<string, string> result;

                try
                {
                    result = consumer.Consume(stoppingToken);
                }
                catch (ConsumeException exception) when (!exception.Error.IsFatal)
                {
                    _logger.LogWarning(exception,
                        "Kafka fetch failed with {Reason}. The consumer will keep polling.",
                        exception.Error.Reason);
                    await DelayBeforeRetryAsync(stoppingToken);
                    continue;
                }

                await ProcessWithRetriesAsync(consumer, deadLetterProducer, result, stoppingToken);
            }
        }
        finally
        {
            consumer.Close();
        }
    }

    private async Task ProcessWithRetriesAsync(
        IConsumer<string, string> consumer,
        IProducer<string, string> deadLetterProducer,
        ConsumeResult<string, string> result,
        CancellationToken cancellationToken)
    {
        Exception? lastException = null;

        for (var attempt = 1; attempt <= _options.Value.MaxProcessingAttempts; attempt++)
        {
            try
            {
                await ProcessEventAsync(result.Message.Value, cancellationToken);

                // A commit is made only after the database work succeeds. If the commit itself
                // fails, the exception leaves this method and the record is safely replayed.
                consumer.Commit(result);

                _logger.LogInformation(
                    "Processed Kafka event {Key} from {TopicPartition} at offset {Offset}.",
                    result.Message.Key,
                    result.TopicPartition,
                    result.Offset);
                return;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                lastException = exception;

                if (attempt < _options.Value.MaxProcessingAttempts)
                {
                    _logger.LogWarning(exception,
                        "Failed to process Kafka event {Key} at offset {Offset} on attempt {Attempt}/{MaxAttempts}.",
                        result.Message.Key,
                        result.Offset,
                        attempt,
                        _options.Value.MaxProcessingAttempts);
                    await DelayBeforeRetryAsync(cancellationToken);
                }
            }
        }

        // The original record is committed only after Kafka acknowledges the DLQ record. If the
        // DLQ publish fails, the exception propagates and the original event remains replayable.
        await PublishToDeadLetterTopicAsync(deadLetterProducer, result, lastException!, cancellationToken);
        consumer.Commit(result);

        _logger.LogError(lastException,
            "Moved Kafka event {Key} at offset {Offset} to dead-letter topic {DeadLetterTopic} after {Attempts} attempts.",
            result.Message.Key,
            result.Offset,
            _options.Value.DeadLetterTopic,
            _options.Value.MaxProcessingAttempts);
    }

    private async Task PublishToDeadLetterTopicAsync(
        IProducer<string, string> producer,
        ConsumeResult<string, string> result,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var deadLetter = new DeadLetterEvent(
            result.Topic,
            result.Partition.Value,
            result.Offset.Value,
            result.Message.Key,
            result.Message.Value,
            exception.GetType().Name,
            exception.Message,
            _options.Value.MaxProcessingAttempts,
            DateTimeOffset.UtcNow);

        await producer.ProduceAsync(_options.Value.DeadLetterTopic, new Message<string, string>
        {
            Key = result.Message.Key,
            Value = JsonSerializer.Serialize(deadLetter, JsonOptions)
        }, cancellationToken);
    }

    private Task DelayBeforeRetryAsync(CancellationToken cancellationToken) =>
        Task.Delay(TimeSpan.FromMilliseconds(_options.Value.RetryDelayMilliseconds), cancellationToken);


    private async Task ProcessEventAsync(
        string message,
        CancellationToken cancellationToken)
    {
        var envelope =
            JsonSerializer.Deserialize<IntegrationEventEnvelope>(message, JsonOptions)
            ?? throw new InvalidOperationException("Kafka event envelope is invalid.");

        using var scope = _scopeFactory.CreateScope();

        var notificationService =
            scope.ServiceProvider
                .GetRequiredService<IAppointmentNotificationApplicationService>();

        switch (envelope.EventType)
        {
            case nameof(AppointmentScheduledEvent):
            {
                var appointmentEvent =
                    Deserialize<AppointmentScheduledEvent>(
                        envelope.Payload);

                await notificationService.HandleAsync(
                    appointmentEvent,
                    cancellationToken);

                break;
            }

            case nameof(AppointmentCancelledEvent):
            {
                var appointmentEvent =
                    Deserialize<AppointmentCancelledEvent>(
                        envelope.Payload);

                await notificationService.HandleAsync(
                    appointmentEvent,
                    cancellationToken);

                break;
            }

            case nameof(AppointmentRescheduledEvent):
            {
                var appointmentEvent =
                    Deserialize<AppointmentRescheduledEvent>(
                        envelope.Payload);

                await notificationService.HandleAsync(
                    appointmentEvent,
                    cancellationToken);

                break;
            }

            default:
                throw new InvalidOperationException(
                    $"Unsupported Kafka event type: {envelope.EventType}");
        }
    }

    private static T Deserialize<T>(string payload)
    {
        return JsonSerializer.Deserialize<T>(payload, JsonOptions)
               ?? throw new InvalidOperationException(
                   $"Could not deserialize {typeof(T).Name}.");
    }

    private sealed record DeadLetterEvent(
        string OriginalTopic,
        int OriginalPartition,
        long OriginalOffset,
        string? Key,
        string Value,
        string ErrorType,
        string ErrorMessage,
        int Attempts,
        DateTimeOffset FailedAtUtc);
}
