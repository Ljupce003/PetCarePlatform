using AppointmentService.Domain.Entities;
using Xunit;

namespace AppointmentService.Domain.Tests;

public sealed class ClinicTests
{
    [Fact]
    public void Constructor_WithValidArguments_TrimsFields()
    {
        var clinic = new Clinic("  Central Vet Clinic  ", "  Skopje  ", "  Bul. Ilinden 1  ");

        Assert.Equal("Central Vet Clinic", clinic.Name);
        Assert.Equal("Skopje", clinic.Location);
        Assert.Equal("Bul. Ilinden 1", clinic.Address);
    }

    [Fact]
    public void Constructor_WithBlankName_Throws()
    {
        Assert.Throws<ArgumentException>(() => new Clinic("   ", "Skopje", "Bul. Ilinden 1"));
    }

    [Fact]
    public void Seed_UsesTheProvidedId()
    {
        var id = Guid.NewGuid();

        var clinic = Clinic.Seed(id, "Central Vet Clinic", "Skopje", "Bul. Ilinden 1");

        Assert.Equal(id, clinic.ClinicId);
    }
}

public sealed class VeterinarianTests
{
    private static readonly Guid ClinicId = Guid.NewGuid();

    [Fact]
    public void Constructor_WithValidArguments_DefaultsToAvailable()
    {
        var veterinarian = new Veterinarian(ClinicId, "Dr. Ana Petrova", "General Practice", "VET-001");

        Assert.True(veterinarian.IsAvailable);
        Assert.Equal(ClinicId, veterinarian.ClinicId);
    }

    [Fact]
    public void Constructor_WithEmptyClinicId_Throws()
    {
        Assert.Throws<ArgumentException>(() => new Veterinarian(Guid.Empty, "Dr. Ana Petrova", "General Practice", "VET-001"));
    }

    [Fact]
    public void Constructor_WithBlankLicenseNumber_Throws()
    {
        Assert.Throws<ArgumentException>(() => new Veterinarian(ClinicId, "Dr. Ana Petrova", "General Practice", "   "));
    }

    [Fact]
    public void MarkUnavailable_ThenMarkAvailable_TogglesIsAvailable()
    {
        var veterinarian = new Veterinarian(ClinicId, "Dr. Ana Petrova", "General Practice", "VET-001");

        veterinarian.MarkUnavailable();
        Assert.False(veterinarian.IsAvailable);

        veterinarian.MarkAvailable();
        Assert.True(veterinarian.IsAvailable);
    }
}
