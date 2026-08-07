using TreatmentAndNotificationService.Domain.Entities;

namespace TreatmentAndNotificationService.Infrastructure.Notifications;

public interface INotificationSender
{
    Task SendAsync(Notification notification, CancellationToken cancellationToken);
}
