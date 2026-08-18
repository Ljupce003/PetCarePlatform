namespace MCPServer.Contracts;

public sealed record VeterinarianResponse(
    Guid VeterinarianId,
    Guid ClinicId,
    string FullName,
    string Specialization,
    bool IsAvailable);
