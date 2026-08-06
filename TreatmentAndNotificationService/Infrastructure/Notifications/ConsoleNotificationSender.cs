using TreatmentAndNotificationService.Domain.Entities;

namespace TreatmentAndNotificationService.Infrastructure.Notifications;

/// <summary>Demo delivery adapter. Replace this adapter with e-mail/SMS providers later.</summary>
public sealed class ConsoleNotificationSender(ILogger<ConsoleNotificationSender> logger) : INotificationSender
{
    public Task SendAsync(Notification notification, CancellationToken cancellationToken)
    {
        logger.LogInformation("Delivering {NotificationType} notification {NotificationId} to owner {OwnerId}: {Title} — {Message}",
            notification.Type, notification.Id, notification.OwnerId, notification.Title, notification.Message);
        return Task.CompletedTask;
    }
}
