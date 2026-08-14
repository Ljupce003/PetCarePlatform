namespace MCPServer.Contracts;

public enum AppointmentStatusResponse
{
    Scheduled = 1,
    Completed = 2,
    Cancelled = 3
}

public sealed record AppointmentResponse(
    Guid AppointmentId,
    Guid PetId,
    Guid OwnerId,
    Guid ClinicId,
    Guid VeterinarianId,
    Guid AvailabilitySlotId,
    DateTimeOffset StartsAtUtc,
    DateTimeOffset EndsAtUtc,
    string Reason,
    AppointmentStatusResponse Status,
    string? CancellationReason,
    DateTimeOffset CreatedAtUtc);
