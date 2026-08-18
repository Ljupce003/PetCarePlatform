using PetService.Domain.Entities;
using PetService.Domain.Enums;
using PetService.Domain.Exceptions;
using PetService.Domain.ValueObjects;

namespace PetService.Domain.Tests;

public sealed class PetTests
{
    [Fact]
    public void Constructor_NormalizesValidPetData()
    {
        var ownerId = Guid.NewGuid();
        var pet = new Pet(
            ownerId, "  Luna  ", PetSpecies.Dog, "  Labrador  ", new DateOnly(2021, 5, 12), 27.5m,
            " mkd000000001 ", [" Chicken ", "chicken"], [" Mild asthma "]);

        Assert.NotEqual(Guid.Empty, pet.PetId);
        Assert.Equal(ownerId, pet.OwnerId);
        Assert.Equal("Luna", pet.Name.Value);
        Assert.Equal("Labrador", pet.Breed);
        Assert.Equal("MKD000000001", pet.MicrochipNumber?.Value);
        Assert.Equal(["Chicken"], pet.Allergies);
        Assert.Equal(["Mild asthma"], pet.ChronicConditions);
    }

    [Fact]
    public void Constructor_WithFutureBirthDate_ThrowsInvalidBirthDateException() =>
        Assert.Throws<InvalidBirthDateException>(() => ValidPet(DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1))));

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Constructor_WithNonPositiveWeight_Throws(decimal weight) =>
        Assert.Throws<ArgumentOutOfRangeException>(() => new Pet(
            Guid.NewGuid(), "Luna", PetSpecies.Dog, null, new DateOnly(2021, 5, 12), weight, null));

    [Fact]
    public void Constructor_WithUnknownSpecies_Throws() =>
        Assert.Throws<ArgumentOutOfRangeException>(() => new Pet(
            Guid.NewGuid(), "Luna", (PetSpecies)999, null, new DateOnly(2021, 5, 12), 10m, null));

    [Theory]
    [InlineData("short")]
    [InlineData("invalid-chip!")]
    public void MicrochipNumber_WithInvalidValue_Throws(string value) =>
        Assert.Throws<InvalidMicrochipException>(() => MicrochipNumber.Create(value));

    [Fact]
    public void PetName_WithBlankValue_Throws() =>
        Assert.Throws<ArgumentException>(() => PetName.Create("   "));

    [Fact]
    public void Update_PreservesIdentityAndChangesMutableData()
    {
        var pet = ValidPet();
        var id = pet.PetId;
        var ownerId = pet.OwnerId;

        pet.Update("Milo", PetSpecies.Cat, null, new DateOnly(2022, 2, 2), 5m, null, [], []);

        Assert.Equal(id, pet.PetId);
        Assert.Equal(ownerId, pet.OwnerId);
        Assert.Equal("Milo", pet.Name.Value);
        Assert.Equal(PetSpecies.Cat, pet.Species);
        Assert.Null(pet.MicrochipNumber);
    }

    private static Pet ValidPet(DateOnly? birthDate = null) => new(
        Guid.NewGuid(), "Luna", PetSpecies.Dog, "Labrador", birthDate ?? new DateOnly(2021, 5, 12), 27.5m,
        "MKD000000001");
}
