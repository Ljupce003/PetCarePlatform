using Shared.AppointmentEvents;
using TreatmentAndNotificationService.Domain.Entities;
using TreatmentAndNotificationService.Domain.Enums;
using TreatmentAndNotificationService.Domain.Repositories;
using TreatmentAndNotificationService.Domain.ValueObjects;

namespace TreatmentAndNotificationService.Application.Services.Impl;

/// <summary>
/// Application event handlers for appointment integration events. A future Kafka consumer
/// should invoke these methods after deserialising and validating the message.
/// </summary>
public sealed class AppointmentNotificationApplicationService(
    INotificationRepository notifications,
    IUnitOfWork unitOfWork) : IAppointmentNotificationApplicationService
{
    public Task HandleAsync(AppointmentScheduledEvent message, CancellationToken ct) => CreateIfNewAsync(
        message.EventId, message.OwnerId, message.PetId, NotificationType.AppointmentScheduled,
        "Appointment scheduled", $"Your veterinary appointment is scheduled for {message.StartsAtUtc:yyyy-MM-dd HH:mm} UTC.",
        message.StartsAtUtc.AddDays(-1) < DateTimeOffset.UtcNow ? DateTimeOffset.UtcNow : message.StartsAtUtc.AddDays(-1), ct);

    public Task HandleAsync(AppointmentCancelledEvent message, CancellationToken ct) => CreateIfNewAsync(
        message.EventId, message.OwnerId, message.PetId, NotificationType.AppointmentCancelled,
        "Appointment cancelled", $"Appointment {message.AppointmentId} was cancelled. {message.CancellationReason}", DateTimeOffset.UtcNow, ct);

    public Task HandleAsync(AppointmentRescheduledEvent message, CancellationToken ct) => CreateIfNewAsync(
        message.EventId, message.OwnerId, message.PetId, NotificationType.AppointmentRescheduled,
        "Appointment rescheduled", $"Your appointment was moved to {message.StartsAtUtc:yyyy-MM-dd HH:mm} UTC.", DateTimeOffset.UtcNow, ct);

    private async Task CreateIfNewAsync(Guid eventId, Guid ownerId, Guid petId, NotificationType type,
        string title, string message, DateTimeOffset scheduledForUtc, CancellationToken ct)
    {
        var source = SourceEventId.Create($"appointment:{type}:{eventId}");
        if (await notifications.ExistsBySourceEventIdAsync(source.Value, ct))
            return;

        await notifications.AddAsync(new Notification(ownerId, petId, type,
            NotificationContent.Create(title, message), scheduledForUtc, source), ct);
        await unitOfWork.SaveChangesAsync(ct);
    }
}
