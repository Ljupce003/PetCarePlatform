using TreatmentAndNotificationService.Domain.Common;

namespace TreatmentAndNotificationService.Domain.ValueObjects;

public sealed record NotificationContent
{
    public const int MaximumTitleLength = 200;
    public const int MaximumMessageLength = 1000;
    public string Title { get; }
    public string Message { get; }

    private NotificationContent(string title, string message) => (Title, Message) = (title, message);

    public static NotificationContent Create(string? title, string? message)
    {
        var normalizedTitle = title?.Trim();
        var normalizedMessage = message?.Trim();
        if (string.IsNullOrWhiteSpace(normalizedTitle) || string.IsNullOrWhiteSpace(normalizedMessage))
            throw new DomainValidationException("Notification title and message are required.");
        if (normalizedTitle.Length > MaximumTitleLength || normalizedMessage.Length > MaximumMessageLength)
            throw new DomainValidationException("Notification content exceeds the allowed length.");
        return new NotificationContent(normalizedTitle, normalizedMessage);
    }
}
