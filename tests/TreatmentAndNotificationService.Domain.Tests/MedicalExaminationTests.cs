using TreatmentAndNotificationService.Domain.Common;
using TreatmentAndNotificationService.Domain.Entities;
using TreatmentAndNotificationService.Domain.Events;
using TreatmentAndNotificationService.Domain.ValueObjects;

namespace TreatmentAndNotificationService.Domain.Tests;

public sealed class MedicalExaminationTests
{
    private static readonly Guid PetId = Guid.NewGuid();
    private static readonly Guid OwnerId = Guid.NewGuid();
    private static readonly Guid VeterinarianId = Guid.NewGuid();

    [Fact]
    public void Constructor_CreatesExaminationAndNormalizesOptionalData()
    {
        var examinedAt = new DateTimeOffset(2026, 8, 10, 12, 0, 0, TimeSpan.FromHours(2));

        var examination = Create(
            examinedAt: examinedAt,
            medications: [" Antibiotic ", "antibiotic", "", "Painkiller"],
            notes: "  Keep hydrated  ");

        Assert.NotEqual(Guid.Empty, examination.Id);
        Assert.Equal(PetId, examination.PetId);
        Assert.Equal(examinedAt.ToUniversalTime(), examination.ExaminedAtUtc);
        Assert.Equal(["Antibiotic", "Painkiller"], examination.Medications);
        Assert.Equal("Keep hydrated", examination.Notes);
        Assert.Empty(examination.DomainEvents);
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
    public void Constructor_RejectsMissingTimeAndInvalidFollowUp()
    {
        var examinedAt = DateTimeOffset.UtcNow;

        Assert.Throws<DomainValidationException>(() => Create(examinedAt: DateTimeOffset.MinValue));
        Assert.Throws<DomainValidationException>(() => Create(examinedAt: examinedAt, nextControl: examinedAt));
        Assert.Throws<DomainValidationException>(() => Create(examinedAt: examinedAt, nextControl: examinedAt.AddMinutes(-1)));
    }

    [Fact]
    public void Constructor_RejectsNullRequiredValueObjectsAndOversizedNotes()
    {
        Assert.Throws<ArgumentNullException>(() => new MedicalExamination(
            PetId, OwnerId, VeterinarianId, null, DateTimeOffset.UtcNow,
            null!, TreatmentPlan.Create("Rest"), null, null, null));
        Assert.Throws<ArgumentNullException>(() => new MedicalExamination(
            PetId, OwnerId, VeterinarianId, null, DateTimeOffset.UtcNow,
            Diagnosis.Create("Allergy"), null!, null, null, null));
        Assert.Throws<DomainValidationException>(() => Create(notes: new string('n', 2001)));
    }

    [Fact]
    public void Constructor_WithFollowUp_RaisesReminderEvent()
    {
        var examinedAt = DateTimeOffset.UtcNow;
        var followUp = examinedAt.AddDays(7);

        var examination = Create(examinedAt: examinedAt, nextControl: followUp);

        var domainEvent = Assert.IsType<FollowUpReminderRequested>(Assert.Single(examination.DomainEvents));
        Assert.Equal(examination.Id, domainEvent.ExaminationId);
        Assert.Equal(OwnerId, domainEvent.OwnerId);
        Assert.Equal(PetId, domainEvent.PetId);
        Assert.Equal(followUp.ToUniversalTime(), domainEvent.FollowUpAtUtc);
    }

    [Fact]
    public void DequeueDomainEvents_ReturnsEventsAndClearsQueue()
    {
        var examination = Create(nextControl: DateTimeOffset.UtcNow.AddDays(1));

        Assert.Single(examination.DequeueDomainEvents());
        Assert.Empty(examination.DomainEvents);
        Assert.Empty(examination.DequeueDomainEvents());
    }

    private static MedicalExamination Create(
        Guid? petId = null,
        Guid? ownerId = null,
        Guid? veterinarianId = null,
        DateTimeOffset? examinedAt = null,
        Diagnosis? diagnosis = null,
        TreatmentPlan? treatmentPlan = null,
        IEnumerable<string>? medications = null,
        DateTimeOffset? nextControl = null,
        string? notes = null) =>
        new(
            petId ?? PetId,
            ownerId ?? OwnerId,
            veterinarianId ?? VeterinarianId,
            Guid.NewGuid(),
            examinedAt ?? DateTimeOffset.UtcNow,
            diagnosis ?? Diagnosis.Create("Allergy"),
            treatmentPlan ?? TreatmentPlan.Create("Rest"),
            medications,
            nextControl,
            notes);
}
