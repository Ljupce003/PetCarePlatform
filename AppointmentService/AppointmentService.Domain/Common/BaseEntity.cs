namespace AppointmentService.Domain.Common;

/// <summary>
/// Base type for every aggregate/entity in the Appointment bounded context.
/// Kept intentionally minimal until the rest of the domain model (Clinic, Veterinarian,
/// AvailabilitySlot, Appointment) is implemented.
/// </summary>
public abstract class BaseEntity
{
    public Guid Id { get; protected set; } = Guid.NewGuid();

    protected BaseEntity()
    {
    }

    protected BaseEntity(Guid id)
    {
        Id = id;
    }
}
