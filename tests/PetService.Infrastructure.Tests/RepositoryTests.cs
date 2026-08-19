using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PetService.Domain.Entities;
using PetService.Domain.Enums;
using PetService.Infrastructure.Persistence;

namespace PetService.Infrastructure.Tests;

public sealed class RepositoryTests
{
    [Fact]
    public void PostgreSqlModel_GeneratesOwnedSchemaWithRequiredIndexesAndArrays()
    {
        var options = new DbContextOptionsBuilder<PetDbContext>()
            .UseNpgsql("Host=localhost;Database=pet-model-test;Username=test;Password=test")
            .Options;
        using var context = new PetDbContext(options);

        var script = context.Database.GenerateCreateScript();

        Assert.Contains("CREATE TABLE owners", script, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("CREATE TABLE pets", script, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("text[]", script, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("IX_pets_MicrochipNumber", script, StringComparison.Ordinal);
        Assert.Contains("UNIQUE", script, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("FOREIGN KEY", script, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task OwnerRepository_PersistsReadsOrdersAndDeletesOwners()
    {
        await using var context = CreateContext();
        var repository = new OwnerRepository(context);
        var zoe = new Owner("Zoe", "zoe@example.com", "+38970111111", null);
        var ana = new Owner("Ana", "ana@example.com", "+38970222222", null);

        await repository.AddAsync(zoe, CancellationToken.None);
        await repository.AddAsync(ana, CancellationToken.None);
        await context.SaveChangesAsync();

        Assert.Equal(zoe.OwnerId, (await repository.GetByIdAsync(zoe.OwnerId, CancellationToken.None))?.OwnerId);
        Assert.Equal(["Ana", "Zoe"], (await repository.GetAllAsync(CancellationToken.None)).Select(owner => owner.OwnerName));

        repository.Remove(zoe);
        await context.SaveChangesAsync();
        Assert.Null(await repository.GetByIdAsync(zoe.OwnerId, CancellationToken.None));
    }

    [Fact]
    public async Task PetRepository_FiltersByOwnerAndDetectsMicrochipConflicts()
    {
        await using var context = CreateContext();
        var owners = new OwnerRepository(context);
        var pets = new PetRepository(context);
        var owner = new Owner("Primary Test Owner", "primary.owner@example.com", "+38970123456", null);
        var otherOwner = new Owner("Other Test Owner", "other.owner@example.com", "+38970987654", null);
        await owners.AddAsync(owner, CancellationToken.None);
        await owners.AddAsync(otherOwner, CancellationToken.None);

        var luna = NewPet(owner.OwnerId, "Luna", "MKD000000001");
        var archie = NewPet(owner.OwnerId, "Archie", "MKD000000002");
        var milo = NewPet(otherOwner.OwnerId, "Milo", "MKD000000003");
        await pets.AddAsync(luna, CancellationToken.None);
        await pets.AddAsync(archie, CancellationToken.None);
        await pets.AddAsync(milo, CancellationToken.None);
        await context.SaveChangesAsync();

        Assert.Equal(["Archie", "Luna"], (await pets.GetByOwnerIdAsync(owner.OwnerId, CancellationToken.None)).Select(pet => pet.Name.Value));
        Assert.True(await pets.ExistsWithMicrochipAsync("mkd000000001", null, CancellationToken.None));
        Assert.False(await pets.ExistsWithMicrochipAsync("MKD000000001", luna.PetId, CancellationToken.None));
        Assert.Equal(["Archie", "Luna", "Milo"], (await pets.GetAllAsync(CancellationToken.None)).Select(pet => pet.Name.Value));
    }

    private static PetDbContext CreateContext()
    {
        var provider = new ServiceCollection().AddEntityFrameworkInMemoryDatabase().BuildServiceProvider();
        var options = new DbContextOptionsBuilder<PetDbContext>()
            .UseInMemoryDatabase($"pet-repository-tests-{Guid.NewGuid():N}")
            .UseInternalServiceProvider(provider)
            .Options;
        return new PetDbContext(options);
    }

    private static Pet NewPet(Guid ownerId, string name, string microchip) => new(
        ownerId, name, PetSpecies.Dog, "Mixed", new DateOnly(2021, 1, 1), 10m, microchip);
}
