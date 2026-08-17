using TreatmentAndNotificationService.Domain.Entities;

namespace TreatmentAndNotificationService.Infrastructure.Notifications;

/// <summary>
/// Final delivery adapter for the course demonstration. Delivery is made observable through
/// structured console logs without requiring external e-mail or SMS accounts.
/// </summary>
public sealed class ConsoleNotificationSender(ILogger<ConsoleNotificationSender> logger) : INotificationSender
{
    public Task SendAsync(Notification notification, CancellationToken cancellationToken)
    {
        logger.LogInformation("Delivering {NotificationType} notification {NotificationId} to owner {OwnerId}: {Title} — {Message}",
            notification.Type, notification.Id, notification.OwnerId, notification.Title, notification.Message);
        return Task.CompletedTask;
    }
}
