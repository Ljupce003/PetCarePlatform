using PetService.Domain.Enums;

namespace PetService.Application.Requests;

public record CreatePetRequest(
    Guid OwnerId,
    string Name,
    PetSpecies Species,
    string? Breed,
    DateOnly BirthDate,
    decimal Weight,
    string? MicrochipNumber,
    IReadOnlyList<string>? Allergies,
    IReadOnlyList<string>? ChronicConditions);

public record UpdatePetRequest(
    string Name,
    PetSpecies Species,
    string? Breed,
    DateOnly BirthDate,
    decimal Weight,
    string? MicrochipNumber,
    IReadOnlyList<string>? Allergies,
    IReadOnlyList<string>? ChronicConditions);
