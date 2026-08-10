namespace TreatmentAndNotificationService.Infrastructure.Messaging;

public sealed class KafkaConsumerOptions
{
    public const string SectionName = "Kafka";

    public string BootstrapServers { get; set; } = "localhost:29092";

    public string GroupId { get; set; } =
        "treatment-notification-service";

    public string Topic { get; set; } =
        "petcare.appointments";

    public string DeadLetterTopic { get; set; } =
        "petcare.appointments.dlq";

    public int MaxProcessingAttempts { get; set; } = 3;

    public int RetryDelayMilliseconds { get; set; } = 1_000;
}
