using PetService.Domain.Enums;

namespace PetService.Application.Dtos;

public record OwnerDto(
    Guid OwnerId,
    string OwnerName,
    string Email,
    string Phone,
    string? Address);

public record PetDto(
    Guid PetId,
    string Name,
    PetSpecies Species,
    string? Breed,
    DateOnly BirthDate,
    decimal Weight,
    string? MicrochipNumber,
    IReadOnlyList<string> Allergies,
    IReadOnlyList<string> ChronicConditions,
    Guid OwnerId);

/// <summary>
/// Minimal integration contract consumed by Appointment Service.
/// </summary>
public record PetOwnershipDto(bool Exists, bool OwnedByOwner);
