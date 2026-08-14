namespace MCPServer.Contracts;

public sealed record PetResponse(
    Guid PetId,
    string Name,
    string Species,
    string? Breed,
    DateOnly BirthDate,
    decimal Weight,
    string? MicrochipNumber,
    IReadOnlyList<string> Allergies,
    IReadOnlyList<string> ChronicConditions,
    Guid OwnerId);
