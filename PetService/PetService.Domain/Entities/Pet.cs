using PetService.Domain.Enums;
using PetService.Domain.Exceptions;
using PetService.Domain.ValueObjects;

namespace PetService.Domain.Entities;

public class Pet
{
    public Guid PetId { get; private set; }
    public PetName Name { get; private set; } = null!;
    public PetSpecies Species { get; private set; }
    public string? Breed { get; private set; }
    public DateOnly BirthDate { get; private set; }
    public decimal Weight { get; private set; }
    public MicrochipNumber? MicrochipNumber { get; private set; }
    public List<string> Allergies { get; private set; } = [];
    public List<string> ChronicConditions { get; private set; } = [];
    public Guid OwnerId { get; private set; }

    // Used by EF Core when loading database records.
    private Pet()
    {
    }

    // Used by our application when creating a new valid owner.
    public Pet(Guid ownerId, string name, PetSpecies species, string? breed, DateOnly birthDate, decimal weight, string? microchipNumber,
        IEnumerable<string>? allergies = null,
        IEnumerable<string>? chronicConditions = null)
    {
        if (ownerId == Guid.Empty)
        {
            throw new ArgumentException("Owner is required.", nameof(ownerId));
        }

        PetId = Guid.NewGuid();
        OwnerId = ownerId;
        Update(name, species, breed, birthDate, weight, microchipNumber, allergies, chronicConditions);
    }

    public void Update(string name, PetSpecies species, string? breed, DateOnly birthDate, decimal weight, string? microchipNumber,
        IEnumerable<string>? allergies,
        IEnumerable<string>? chronicConditions)
    {
        if (!Enum.IsDefined(species))
        {
            throw new ArgumentOutOfRangeException(nameof(species), species, "A valid pet species is required.");
        }

        if (birthDate == default || birthDate > DateOnly.FromDateTime(DateTime.UtcNow))
        {
            throw new InvalidBirthDateException(birthDate);
        }

        if (weight <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(weight), weight, "Pet weight must be greater than zero.");
        }

        var petName = PetName.Create(name);
        var normalizedMicrochipNumber = PetService.Domain.ValueObjects.MicrochipNumber.Create(microchipNumber);
        var normalizedAllergies = Normalize(allergies);
        var normalizedChronicConditions = Normalize(chronicConditions);

        Name = petName;
        Species = species;
        Breed = string.IsNullOrWhiteSpace(breed) ? null : breed.Trim();
        BirthDate = birthDate;
        Weight = weight;
        MicrochipNumber = normalizedMicrochipNumber;
        Allergies = normalizedAllergies;
        ChronicConditions = normalizedChronicConditions;
    }

    private static List<string> Normalize(IEnumerable<string>? values) => values?
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList() ?? [];
}
