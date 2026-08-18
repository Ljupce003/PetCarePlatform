namespace Shared.AppointmentEvents;

public record AppointmentScheduledEvent(
    Guid EventId,
    Guid AppointmentId,
    Guid PetId,
    Guid OwnerId,
    Guid VeterinarianId,
    DateTimeOffset StartsAtUtc,
    DateTimeOffset EndsAtUtc,
    string Reason);