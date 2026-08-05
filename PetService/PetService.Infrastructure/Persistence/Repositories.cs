using Microsoft.EntityFrameworkCore;
using PetService.Application.Abstractions;
using PetService.Domain.Entities;
using PetService.Domain.ValueObjects;

namespace PetService.Infrastructure.Persistence;

public class OwnerRepository(PetDbContext dbContext) : IOwnerRepository
{
    public async Task<Owner?> GetByIdAsync(Guid ownerId, CancellationToken cancellationToken) =>
        await dbContext.Owners.FirstOrDefaultAsync(owner => owner.OwnerId == ownerId, cancellationToken);

    public async Task<IReadOnlyList<Owner>> GetAllAsync(CancellationToken cancellationToken) =>
        await dbContext.Owners
            .AsNoTracking()
            .OrderBy(owner => owner.OwnerName)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(Owner owner, CancellationToken cancellationToken) =>
        await dbContext.Owners.AddAsync(owner, cancellationToken);

    public void Remove(Owner owner) => dbContext.Owners.Remove(owner);
}

public class PetRepository(PetDbContext dbContext) : IPetRepository
{
    public async Task<Pet?> GetByIdAsync(Guid petId, CancellationToken cancellationToken) =>
        await dbContext.Pets.FirstOrDefaultAsync(pet => pet.PetId == petId, cancellationToken);

    public async Task<IReadOnlyList<Pet>> GetAllAsync(CancellationToken cancellationToken) =>
        await dbContext.Pets
            .AsNoTracking()
            .OrderBy(pet => pet.Name)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Pet>> GetByOwnerIdAsync(Guid ownerId, CancellationToken cancellationToken) =>
        await dbContext.Pets
            .AsNoTracking()
            .Where(pet => pet.OwnerId == ownerId)
            .OrderBy(pet => pet.Name)
            .ToListAsync(cancellationToken);

    public async Task<bool> ExistsWithMicrochipAsync(
        string microchipNumber,
        Guid? excludingPetId,
        CancellationToken cancellationToken)
    {
        var normalized = MicrochipNumber.Create(microchipNumber)!;

        return await dbContext.Pets
            .AsNoTracking()
            .AnyAsync(
                pet => pet.MicrochipNumber == normalized
                    && (!excludingPetId.HasValue || pet.PetId != excludingPetId.Value),
                cancellationToken);
    }

    public async Task AddAsync(Pet pet, CancellationToken cancellationToken) =>
        await dbContext.Pets.AddAsync(pet, cancellationToken);

    public void Remove(Pet pet) => dbContext.Pets.Remove(pet);
}
