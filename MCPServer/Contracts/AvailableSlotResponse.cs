namespace MCPServer.Contracts;

public sealed record AvailableSlotResponse(
    Guid AvailabilitySlotId,
    Guid VeterinarianId,
    string VeterinarianName,
    string Specialization,
    Guid ClinicId,
    string ClinicName,
    DateTimeOffset StartsAtUtc,
    DateTimeOffset EndsAtUtc);
