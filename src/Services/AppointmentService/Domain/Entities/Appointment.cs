using AppointmentService.Domain.Enums;
using AppointmentService.Domain.Exceptions;

namespace AppointmentService.Domain.Entities;

public class Appointment
{
    public Guid AppointmentId { get; private set; }
    public Guid PetId { get; private set; }
    public Guid OwnerId { get; private set; }
    public Guid ClinicId { get; private set; }
    public Guid VeterinarianId { get; private set; }
    public Guid AvailabilitySlotId { get; private set; }
    public DateTimeOffset StartsAtUtc { get; private set; }
    public DateTimeOffset EndsAtUtc { get; private set; }
    public string Reason { get; private set; } = string.Empty;
    public AppointmentStatus Status { get; private set; }
    public string? CancellationReason { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }

    // Used by EF Core when loading database records.
    private Appointment()
    {
    }

    // Used by our application once pet ownership has been verified and a slot has been reserved.
    public Appointment(
        Guid petId,
        Guid ownerId,
        Guid clinicId,
        Guid veterinarianId,
        Guid availabilitySlotId,
        DateTimeOffset startsAtUtc,
        DateTimeOffset endsAtUtc,
        string reason)
    {
        if (petId == Guid.Empty)
        {
            throw new ArgumentException("Pet is required.", nameof(petId));
        }

        if (ownerId == Guid.Empty)
        {
            throw new ArgumentException("Owner is required.", nameof(ownerId));
        }

        if (clinicId == Guid.Empty)
        {
            throw new ArgumentException("Clinic is required.", nameof(clinicId));
        }

        if (veterinarianId == Guid.Empty)
        {
            throw new ArgumentException("Veterinarian is required.", nameof(veterinarianId));
        }

        if (availabilitySlotId == Guid.Empty)
        {
            throw new ArgumentException("Availability slot is required.", nameof(availabilitySlotId));
        }

        if (startsAtUtc >= endsAtUtc)
        {
            throw new ArgumentException("Appointment end must be after appointment start.", nameof(endsAtUtc));
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new ArgumentException("Appointment reason is required.", nameof(reason));
        }

        AppointmentId = Guid.NewGuid();
        PetId = petId;
        OwnerId = ownerId;
        ClinicId = clinicId;
        VeterinarianId = veterinarianId;
        AvailabilitySlotId = availabilitySlotId;
        StartsAtUtc = startsAtUtc;
        EndsAtUtc = endsAtUtc;
        Reason = reason.Trim();
        Status = AppointmentStatus.Scheduled;
        CreatedAtUtc = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Only a still-scheduled appointment can be cancelled — a completed or already-cancelled
    /// appointment is final. Releasing the underlying <see cref="AvailabilitySlot"/> is the
    /// application layer's responsibility, since this entity doesn't hold a reference to it.
    /// </summary>
    public void Cancel(string? reason)
    {
        EnsureCanTransitionFromScheduled(nameof(Cancel));

        Status = AppointmentStatus.Cancelled;
        CancellationReason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();
    }

    /// <summary>
    /// Moves a still-scheduled appointment onto a different slot/veterinarian/time. Releasing the
    /// old slot and reserving the new one are the application layer's responsibility — this only
    /// updates the appointment's own record once that has happened.
    /// </summary>
    public void Reschedule(Guid newAvailabilitySlotId, Guid veterinarianId, DateTimeOffset startsAtUtc, DateTimeOffset endsAtUtc)
    {
        EnsureCanTransitionFromScheduled(nameof(Reschedule));

        if (newAvailabilitySlotId == Guid.Empty)
        {
            throw new ArgumentException("Availability slot is required.", nameof(newAvailabilitySlotId));
        }

        if (veterinarianId == Guid.Empty)
        {
            throw new ArgumentException("Veterinarian is required.", nameof(veterinarianId));
        }

        if (startsAtUtc >= endsAtUtc)
        {
            throw new ArgumentException("Appointment end must be after appointment start.", nameof(endsAtUtc));
        }

        AvailabilitySlotId = newAvailabilitySlotId;
        VeterinarianId = veterinarianId;
        StartsAtUtc = startsAtUtc;
        EndsAtUtc = endsAtUtc;
    }

    /// <summary>Only a scheduled appointment can be marked completed.</summary>
    public void Complete()
    {
        EnsureCanTransitionFromScheduled(nameof(Complete));
        Status = AppointmentStatus.Completed;
    }

    private void EnsureCanTransitionFromScheduled(string action)
    {
        if (Status != AppointmentStatus.Scheduled)
        {
            throw new InvalidAppointmentStatusTransitionException(AppointmentId, Status, action);
        }
    }
}
