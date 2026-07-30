namespace Shared.AppointmentEvents;

public record AppointmentCancelledEvent(
    Guid EventId,
    Guid AppointmentId,
    Guid PetId,
    Guid OwnerId,
    DateTimeOffset CancelledAtUtc,
    string? CancellationReason);