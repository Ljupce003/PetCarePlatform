using PetService.Domain.Entities;

namespace PetService.Application.Abstractions;

public interface IOwnerRepository
{
    Task<Owner?> GetByIdAsync(Guid ownerId, CancellationToken cancellationToken);

    Task AddAsync(Owner owner, CancellationToken cancellationToken);

    void Remove(Owner owner);
}

public interface IPetRepository
{
    Task<Pet?> GetByIdAsync(Guid petId, CancellationToken cancellationToken);

    Task<IReadOnlyList<Pet>> GetAllAsync(CancellationToken cancellationToken);

    Task<IReadOnlyList<Pet>> GetByOwnerIdAsync(Guid ownerId, CancellationToken cancellationToken);

    Task<bool> ExistsWithMicrochipAsync(
        string microchipNumber,
        Guid? excludingPetId,
        CancellationToken cancellationToken);

    Task AddAsync(Pet pet, CancellationToken cancellationToken);

    void Remove(Pet pet);
}

/// <summary>
/// Commits all changes made during one application use case as a single transaction.
/// </summary>
public interface IUnitOfWork
{
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
