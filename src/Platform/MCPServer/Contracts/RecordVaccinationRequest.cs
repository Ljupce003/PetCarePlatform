namespace MCPServer.Contracts;

public record RecordVaccinationRequest(
    Guid PetId,
    Guid OwnerId,
    Guid VeterinarianId,
    string VaccineName,
    DateOnly AdministeredOn,
    DateOnly? NextDueOn,
    string? BatchNumber);