using TreatmentAndNotificationService.Domain.Entities;
using TreatmentAndNotificationService.Domain.Enums;
using TreatmentAndNotificationService.Domain.Events;
using TreatmentAndNotificationService.Domain.Repositories;
using TreatmentAndNotificationService.Domain.ValueObjects;

namespace TreatmentAndNotificationService.Application.Events;

public sealed class VaccinationReminderRequestedHandler(INotificationRepository notifications)
{
    public Task HandleAsync(VaccinationReminderRequested domainEvent, CancellationToken cancellationToken)
    {
        var scheduledFor = new DateTimeOffset(domainEvent.DueOn.ToDateTime(new TimeOnly(9, 0)), TimeSpan.Zero).AddDays(-7);
        if (scheduledFor < DateTimeOffset.UtcNow)
            scheduledFor = DateTimeOffset.UtcNow;

        var notification = new Notification(
            domainEvent.OwnerId, domainEvent.PetId, NotificationType.VaccinationReminder,
            NotificationContent.Create("Vaccination reminder", $"{domainEvent.VaccineName} is due on {domainEvent.DueOn:yyyy-MM-dd}."),
            scheduledFor, SourceEventId.Create($"vaccination:{domainEvent.VaccinationId}"));
        return notifications.AddAsync(notification, cancellationToken);
    }
}
