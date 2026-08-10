using TreatmentAndNotificationService.Domain.Common;
using TreatmentAndNotificationService.Domain.Entities;
using TreatmentAndNotificationService.Domain.Events;
using TreatmentAndNotificationService.Domain.ValueObjects;

namespace TreatmentAndNotificationService.Domain.Tests;

public sealed class VaccinationTests
{
    private static readonly Guid PetId = Guid.NewGuid();
    private static readonly Guid OwnerId = Guid.NewGuid();
    private static readonly Guid VeterinarianId = Guid.NewGuid();
    private static readonly DateOnly AdministeredOn = new(2026, 8, 10);

    [Fact]
    public void Constructor_CreatesVaccinationAndNormalizesBatchNumber()
    {
        var vaccination = Create(batchNumber: "  LOT-123  ");

        Assert.NotEqual(Guid.Empty, vaccination.Id);
        Assert.Equal(PetId, vaccination.PetId);
        Assert.Equal("Rabies", vaccination.VaccineName.Value);
        Assert.Equal(AdministeredOn, vaccination.AdministeredOn);
        Assert.Equal("LOT-123", vaccination.BatchNumber);
        Assert.Empty(vaccination.DomainEvents);
    }

    [Theory]
    [InlineData("pet")]
    [InlineData("owner")]
    [InlineData("veterinarian")]
    public void Constructor_WhenRequiredIdentifierIsMissing_Throws(string missing)
    {
        var petId = missing == "pet" ? Guid.Empty : PetId;
        var ownerId = missing == "owner" ? Guid.Empty : OwnerId;
        var veterinarianId = missing == "veterinarian" ? Guid.Empty : VeterinarianId;

        Assert.Throws<DomainValidationException>(() => Create(petId, ownerId, veterinarianId));
    }

    [Fact]
    public void Constructor_RejectsNullValueObjectsAndOversizedBatchNumber()
    {
        Assert.Throws<ArgumentNullException>(() => new Vaccination(
            PetId, OwnerId, VeterinarianId, null!, VaccinationSchedule.Create(AdministeredOn, null), null));
        Assert.Throws<ArgumentNullException>(() => new Vaccination(
            PetId, OwnerId, VeterinarianId, VaccineName.Create("Rabies"), null!, null));
        Assert.Throws<DomainValidationException>(() => Create(batchNumber: new string('b', 101)));
    }

    [Fact]
    public void Constructor_TreatsWhitespaceBatchNumberAsMissing()
    {
        Assert.Null(Create(batchNumber: "  ").BatchNumber);
    }

    [Fact]
    public void Constructor_WithNextDose_RaisesReminderEvent()
    {
        var nextDue = AdministeredOn.AddYears(1);

        var vaccination = Create(schedule: VaccinationSchedule.Create(AdministeredOn, nextDue));

        var domainEvent = Assert.IsType<VaccinationReminderRequested>(Assert.Single(vaccination.DomainEvents));
        Assert.Equal(vaccination.Id, domainEvent.VaccinationId);
        Assert.Equal(OwnerId, domainEvent.OwnerId);
        Assert.Equal(PetId, domainEvent.PetId);
        Assert.Equal("Rabies", domainEvent.VaccineName);
        Assert.Equal(nextDue, domainEvent.DueOn);
    }

    [Fact]
    public void DequeueDomainEvents_ReturnsEventsAndClearsQueue()
    {
        var vaccination = Create(schedule: VaccinationSchedule.Create(AdministeredOn, AdministeredOn.AddYears(1)));

        Assert.Single(vaccination.DequeueDomainEvents());
        Assert.Empty(vaccination.DomainEvents);
        Assert.Empty(vaccination.DequeueDomainEvents());
    }

    private static Vaccination Create(
        Guid? petId = null,
        Guid? ownerId = null,
        Guid? veterinarianId = null,
        VaccineName? vaccineName = null,
        VaccinationSchedule? schedule = null,
        string? batchNumber = null) =>
        new(
            petId ?? PetId,
            ownerId ?? OwnerId,
            veterinarianId ?? VeterinarianId,
            vaccineName ?? VaccineName.Create("Rabies"),
            schedule ?? VaccinationSchedule.Create(AdministeredOn, null),
            batchNumber);
}
