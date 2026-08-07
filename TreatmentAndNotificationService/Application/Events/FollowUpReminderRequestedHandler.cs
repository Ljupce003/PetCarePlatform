using TreatmentAndNotificationService.Domain.Entities;
using TreatmentAndNotificationService.Domain.Enums;
using TreatmentAndNotificationService.Domain.Events;
using TreatmentAndNotificationService.Domain.Repositories;
using TreatmentAndNotificationService.Domain.ValueObjects;

namespace TreatmentAndNotificationService.Application.Events;

public sealed class FollowUpReminderRequestedHandler(INotificationRepository notifications)
{
    public Task HandleAsync(FollowUpReminderRequested domainEvent, CancellationToken cancellationToken)
    {
        var scheduledFor = domainEvent.FollowUpAtUtc.AddDays(-1);
        if (scheduledFor < DateTimeOffset.UtcNow)
            scheduledFor = DateTimeOffset.UtcNow;

        var notification = new Notification(
            domainEvent.OwnerId, domainEvent.PetId, NotificationType.FollowUpReminder,
            NotificationContent.Create("Veterinary follow-up", $"A follow-up visit is recommended on {domainEvent.FollowUpAtUtc:yyyy-MM-dd HH:mm} UTC."),
            scheduledFor, SourceEventId.Create($"examination:{domainEvent.ExaminationId}"));
        return notifications.AddAsync(notification, cancellationToken);
    }
}
