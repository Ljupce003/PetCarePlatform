using TreatmentAndNotificationService.Domain.Repositories;

namespace TreatmentAndNotificationService.Infrastructure.Notifications;

/// <summary>
/// Executes one notification-delivery cycle. The hosted worker owns scheduling while this
/// processor owns the testable fetch, delivery, state-transition, and persistence workflow.
/// </summary>
public sealed class NotificationDeliveryProcessor(
    INotificationRepository notifications,
    IUnitOfWork unitOfWork,
    INotificationSender sender,
    TimeProvider timeProvider,
    ILogger<NotificationDeliveryProcessor> logger)
{
    public async Task<int> DeliverDueAsync(int batchSize, CancellationToken cancellationToken)
    {
        if (batchSize <= 0)
            throw new ArgumentOutOfRangeException(nameof(batchSize), "Batch size must be greater than zero.");

        var due = await notifications.GetDuePendingAsync(
            timeProvider.GetUtcNow(), batchSize, cancellationToken);

        foreach (var notification in due)
        {
            try
            {
                await sender.SendAsync(notification, cancellationToken);
                notification.MarkSent(timeProvider.GetUtcNow());
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                logger.LogError(exception,
                    "Unable to deliver notification {NotificationId}.", notification.Id);
                notification.MarkFailed(exception.Message);
            }
        }

        if (due.Count > 0)
            await unitOfWork.SaveChangesAsync(cancellationToken);

        return due.Count;
    }
}
