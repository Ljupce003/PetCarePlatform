using PetService.Application.Requests;
using PetService.Domain.Entities;

namespace PetService.Application.Dtos;

public static class Mappings
{
    public static OwnerDto ToDto(this Owner owner) =>
        new(owner.OwnerId, owner.OwnerName, owner.Email, owner.Phone, owner.Address);

    public static PetDto ToDto(this Pet pet) =>
        new(
            pet.PetId,
            pet.Name.Value,
            pet.Species,
            pet.Breed,
            pet.BirthDate,
            pet.Weight,
            pet.MicrochipNumber?.Value,
            pet.Allergies.ToArray(),
            pet.ChronicConditions.ToArray(),
            pet.OwnerId);

    /// <summary>
    /// Maps an application input model into a domain entity. The entity constructor remains
    /// the final authority for business invariants.
    /// </summary>
    public static Pet ToEntity(this CreatePetRequest request) =>
        new(
            request.OwnerId,
            request.Name,
            request.Species,
            request.Breed,
            request.BirthDate,
            request.Weight,
            request.MicrochipNumber,
            request.Allergies,
            request.ChronicConditions);

    public static void ApplyTo(this UpdatePetRequest request, Pet pet) =>
        pet.Update(
            request.Name,
            request.Species,
            request.Breed,
            request.BirthDate,
            request.Weight,
            request.MicrochipNumber,
            request.Allergies,
            request.ChronicConditions);
}
