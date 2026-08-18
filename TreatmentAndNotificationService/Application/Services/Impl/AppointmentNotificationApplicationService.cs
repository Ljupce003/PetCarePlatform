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
    public Task HandleAsync(AppointmentScheduledEvent message, CancellationToken ct) => CreateForParticipantsAsync(
        message.EventId, message.OwnerId, message.PetId, message.VeterinarianId, NotificationType.AppointmentScheduled,
        "Appointment scheduled", $"Your veterinary appointment is scheduled for {message.StartsAtUtc:yyyy-MM-dd HH:mm} UTC.",
        "Availability slot booked", $"A patient booked your availability for {message.StartsAtUtc:yyyy-MM-dd HH:mm} UTC.",
        message.StartsAtUtc.AddDays(-1) < DateTimeOffset.UtcNow ? DateTimeOffset.UtcNow : message.StartsAtUtc.AddDays(-1), ct);

    public Task HandleAsync(AppointmentCancelledEvent message, CancellationToken ct) => CreateForParticipantsAsync(
        message.EventId, message.OwnerId, message.PetId, message.VeterinarianId, NotificationType.AppointmentCancelled,
        "Appointment cancelled", $"Appointment {message.AppointmentId} was cancelled. {message.CancellationReason}",
        "Availability slot reopened", $"A patient cancelled the appointment at {message.CancelledAtUtc:yyyy-MM-dd HH:mm} UTC; the slot is open again.",
        DateTimeOffset.UtcNow, ct);

    public Task HandleAsync(AppointmentRescheduledEvent message, CancellationToken ct) => CreateForParticipantsAsync(
        message.EventId, message.OwnerId, message.PetId, message.VeterinarianId, NotificationType.AppointmentRescheduled,
        "Appointment rescheduled", $"Your appointment was moved to {message.StartsAtUtc:yyyy-MM-dd HH:mm} UTC.",
        "Appointment moved", $"A patient is now scheduled with you for {message.StartsAtUtc:yyyy-MM-dd HH:mm} UTC.",
        DateTimeOffset.UtcNow, ct);

    private async Task CreateForParticipantsAsync(Guid eventId, Guid ownerId, Guid petId, Guid veterinarianId,
        NotificationType type, string ownerTitle, string ownerMessage, string veterinarianTitle, string veterinarianMessage,
        DateTimeOffset scheduledForUtc, CancellationToken ct)
    {
        await CreateIfNewAsync(eventId, "owner", ownerId, petId, null, type, ownerTitle, ownerMessage, scheduledForUtc, ct);
        await CreateIfNewAsync(eventId, "veterinarian", ownerId, petId, veterinarianId, type, veterinarianTitle, veterinarianMessage, scheduledForUtc, ct);
    }

    private async Task CreateIfNewAsync(Guid eventId, string recipient, Guid ownerId, Guid petId, Guid? veterinarianId, NotificationType type,
        string title, string message, DateTimeOffset scheduledForUtc, CancellationToken ct)
    {
        var source = SourceEventId.Create($"appointment:{type}:{eventId}:{recipient}");
        if (await notifications.ExistsBySourceEventIdAsync(source.Value, ct))
            return;

        await notifications.AddAsync(new Notification(ownerId, petId, type,
            NotificationContent.Create(title, message), scheduledForUtc, source, veterinarianId), ct);
        await unitOfWork.SaveChangesAsync(ct);
    }
}
