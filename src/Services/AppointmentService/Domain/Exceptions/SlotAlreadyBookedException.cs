namespace AppointmentService.Domain.Exceptions;

public class SlotAlreadyBookedException : Exception
{
    public SlotAlreadyBookedException(Guid slotId)
        : base($"Availability slot '{slotId}' is already booked.")
    {
        SlotId = slotId;
    }

    public Guid SlotId { get; }
}
