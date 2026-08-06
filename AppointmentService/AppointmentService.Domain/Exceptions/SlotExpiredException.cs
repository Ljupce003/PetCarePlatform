namespace AppointmentService.Domain.Exceptions;

public class SlotExpiredException : Exception
{
    public SlotExpiredException(Guid slotId)
        : base($"Availability slot '{slotId}' has already passed and can no longer be booked.")
    {
        SlotId = slotId;
    }

    public Guid SlotId { get; }
}
