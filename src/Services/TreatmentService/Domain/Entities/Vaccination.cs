using TreatmentAndNotificationService.Domain.Common;
using TreatmentAndNotificationService.Domain.Events;
using TreatmentAndNotificationService.Domain.ValueObjects;
using System.ComponentModel.DataAnnotations.Schema;

namespace TreatmentAndNotificationService.Domain.Entities;

/// <summary>Aggregate root representing one administered vaccine.</summary>
public class Vaccination
{
    private readonly List<IDomainEvent> _domainEvents = [];

    public Guid Id { get; private set; }
    public Guid PetId { get; private set; }
    public Guid OwnerId { get; private set; }
    public Guid VeterinarianId { get; private set; }
    public VaccineName VaccineName { get; private set; } = null!;
    public DateOnly AdministeredOn { get; private set; }
    public DateOnly? NextDueOn { get; private set; }
    public string? BatchNumber { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    [NotMapped]
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    private Vaccination() { }

    public Vaccination(Guid petId, Guid ownerId, Guid veterinarianId, VaccineName vaccineName,
        VaccinationSchedule schedule, string? batchNumber)
    {
        if (petId == Guid.Empty || ownerId == Guid.Empty || veterinarianId == Guid.Empty)
            throw new DomainValidationException("Pet, owner and veterinarian are required.");

        Id = Guid.NewGuid();
        PetId = petId;
        OwnerId = ownerId;
        VeterinarianId = veterinarianId;
        VaccineName = vaccineName ?? throw new ArgumentNullException(nameof(vaccineName));
        ArgumentNullException.ThrowIfNull(schedule);
        AdministeredOn = schedule.AdministeredOn;
        NextDueOn = schedule.NextDueOn;
        BatchNumber = NormalizeBatchNumber(batchNumber);
        CreatedAtUtc = DateTimeOffset.UtcNow;

        if (NextDueOn.HasValue)
            _domainEvents.Add(new VaccinationReminderRequested(
                Id, OwnerId, PetId, VaccineName.Value, NextDueOn.Value, CreatedAtUtc));
    }

    public IReadOnlyCollection<IDomainEvent> DequeueDomainEvents()
    {
        var events = _domainEvents.ToArray();
        _domainEvents.Clear();
        return events;
    }

    public void Update(VaccineName vaccineName, VaccinationSchedule schedule, string? batchNumber)
    {
        VaccineName = vaccineName ?? throw new ArgumentNullException(nameof(vaccineName));
        ArgumentNullException.ThrowIfNull(schedule);
        AdministeredOn = schedule.AdministeredOn;
        NextDueOn = schedule.NextDueOn;
        BatchNumber = NormalizeBatchNumber(batchNumber);
    }

    private static string? NormalizeBatchNumber(string? value)
    {
        var normalized = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        if (normalized?.Length > 100)
            throw new DomainValidationException("Batch number cannot exceed 100 characters.");
        return normalized;
    }
}
