namespace Shared.AppointmentEvents;

public record AppointmentRescheduledEvent(
    Guid EventId,
    Guid AppointmentId,
    Guid PetId,
    Guid OwnerId,
    Guid VeterinarianId,
    DateTimeOffset StartsAtUtc,
    DateTimeOffset EndsAtUtc);