using TreatmentAndNotificationService.Domain.Repositories;

namespace TreatmentAndNotificationService.Infrastructure.Notifications;

public sealed class NotificationDeliveryWorker(
    IServiceScopeFactory scopeFactory,
    ILogger<NotificationDeliveryWorker> logger) : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(15);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await DeliverDueNotificationsAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { }
            catch (Exception exception)
            {
                logger.LogError(exception, "Notification delivery cycle failed.");
            }

            await Task.Delay(PollInterval, stoppingToken);
        }
    }

    private async Task DeliverDueNotificationsAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var notifications = scope.ServiceProvider.GetRequiredService<INotificationRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var sender = scope.ServiceProvider.GetRequiredService<INotificationSender>();
        var due = await notifications.GetDuePendingAsync(DateTimeOffset.UtcNow, 100, cancellationToken);

        foreach (var notification in due)
        {
            try
            {
                await sender.SendAsync(notification, cancellationToken);
                notification.MarkSent(DateTimeOffset.UtcNow);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                logger.LogError(exception, "Unable to deliver notification {NotificationId}.", notification.Id);
                notification.MarkFailed(exception.Message);
            }
        }

        if (due.Count > 0)
            await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
