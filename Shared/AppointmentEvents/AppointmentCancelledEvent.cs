namespace Shared.AppointmentEvents;

public record AppointmentCancelledEvent(
    Guid EventId,
    Guid AppointmentId,
    Guid PetId,
    Guid OwnerId,
    Guid VeterinarianId,
    DateTimeOffset CancelledAtUtc,
    string? CancellationReason);
