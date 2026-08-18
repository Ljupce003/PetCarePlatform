using Microsoft.EntityFrameworkCore;
using PetService.Domain.Entities;
using PetService.Domain.Enums;

namespace PetService.Infrastructure.Persistence;

/// <summary>
/// Applies Pet Service migrations and adds deterministic demo data when its dedicated
/// database is empty.
/// </summary>
public static class PetDbInitializer
{
    // These match AppointmentDbInitializer's demo owner and pet, allowing the seeded
    // Appointment Service booking to be verified against the seeded Pet Service data.
    public static readonly Guid DemoOwnerId = new("33333333-3333-3333-3333-333333333333");
    public static readonly Guid DemoSecondOwnerId = new("33333333-3333-3333-3333-333333333334");
    public static readonly Guid DemoPetId = new("44444444-4444-4444-4444-444444444444");
    public static readonly Guid DemoSecondPetId = new("44444444-4444-4444-4444-444444444445");

    public static async Task InitializeAsync(PetDbContext dbContext, CancellationToken cancellationToken = default)
    {
        await dbContext.Database.MigrateAsync(cancellationToken);

        if (await dbContext.Owners.AnyAsync(cancellationToken))
        {
            return;
        }

        await SeedAsync(dbContext, cancellationToken);
    }

    private static async Task SeedAsync(PetDbContext dbContext, CancellationToken cancellationToken)
    {
        var owner = Owner.Seed(
            DemoOwnerId,
            "Elena Petrova",
            "elena.petcare@example.com",
            "+389 70 123 456",
            "Partizanski Odredi 42, Skopje");

        var secondOwner = Owner.Seed(
            DemoSecondOwnerId,
            "Marko Stojanov",
            "marko.petcare@example.com",
            "+389 71 987 654",
            "Jane Sandanski 18, Skopje");

        var pet = Pet.Seed(
            DemoPetId,
            owner.OwnerId,
            "Luna",
            PetSpecies.Dog,
            "Labrador Retriever",
            new DateOnly(2021, 5, 12),
            27.5m,
            "MKD000000001",
            ["Chicken"],
            []);

        var secondPet = Pet.Seed(
            DemoSecondPetId,
            secondOwner.OwnerId,
            "Milo",
            PetSpecies.Cat,
            "European Shorthair",
            new DateOnly(2022, 9, 3),
            4.8m,
            "MKC000000002",
            [],
            ["Mild asthma"]);

        dbContext.Owners.AddRange(owner, secondOwner);
        dbContext.Pets.AddRange(pet, secondPet);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
