using AppointmentService.Domain.Exceptions;

namespace AppointmentService.Domain.Entities;

public class AvailabilitySlot
{
    public Guid AvailabilitySlotId { get; private set; }
    public Guid VeterinarianId { get; private set; }
    public DateTimeOffset StartsAtUtc { get; private set; }
    public DateTimeOffset EndsAtUtc { get; private set; }
    public bool IsBooked { get; private set; }

    // Used by EF Core when loading database records.
    private AvailabilitySlot()
    {
    }

    // Used by our application when creating a new valid slot.
    public AvailabilitySlot(Guid veterinarianId, DateTimeOffset startsAtUtc, DateTimeOffset endsAtUtc)
    {
        if (veterinarianId == Guid.Empty)
        {
            throw new ArgumentException("Veterinarian is required.", nameof(veterinarianId));
        }

        if (startsAtUtc >= endsAtUtc)
        {
            throw new ArgumentException("Slot end must be after slot start.", nameof(endsAtUtc));
        }

        AvailabilitySlotId = Guid.NewGuid();
        VeterinarianId = veterinarianId;
        StartsAtUtc = startsAtUtc;
        EndsAtUtc = endsAtUtc;
    }

    /// <summary>
    /// Marks the slot booked for an appointment. This is the single place double-booking and
    /// booking an expired slot are prevented — callers (application layer) never need to
    /// duplicate these checks.
    /// </summary>
    public void Reserve()
    {
        if (IsBooked)
        {
            throw new SlotAlreadyBookedException(AvailabilitySlotId);
        }

        if (StartsAtUtc <= DateTimeOffset.UtcNow)
        {
            throw new SlotExpiredException(AvailabilitySlotId);
        }

        IsBooked = true;
    }

    /// <summary>
    /// Frees the slot back up, e.g. when the appointment holding it is cancelled, or when it is
    /// rescheduled onto a different slot.
    /// </summary>
    public void Release() => IsBooked = false;

    public void Update(DateTimeOffset startsAtUtc, DateTimeOffset endsAtUtc)
    {
        if (IsBooked) throw new InvalidOperationException("A booked slot cannot be changed.");
        if (startsAtUtc <= DateTimeOffset.UtcNow) throw new SlotExpiredException(AvailabilitySlotId);
        if (startsAtUtc >= endsAtUtc) throw new ArgumentException("Slot end must be after slot start.", nameof(endsAtUtc));
        StartsAtUtc = startsAtUtc;
        EndsAtUtc = endsAtUtc;
    }
}
