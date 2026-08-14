namespace MCPServer.Contracts;

public record VaccinationResponse(
    Guid Id,
    Guid PetId,
    Guid OwnerId,
    Guid VeterinarianId,
    string VaccineName,
    DateOnly AdministeredOn,
    DateOnly? NextDueOn,
    string? BatchNumber);