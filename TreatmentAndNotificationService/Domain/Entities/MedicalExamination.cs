using TreatmentAndNotificationService.Domain.Common;
using TreatmentAndNotificationService.Domain.Events;
using TreatmentAndNotificationService.Domain.ValueObjects;
using System.ComponentModel.DataAnnotations.Schema;

namespace TreatmentAndNotificationService.Domain.Entities;

/// <summary>Aggregate root for an immutable clinical examination record.</summary>
public class MedicalExamination
{
    private readonly List<IDomainEvent> _domainEvents = [];

    public Guid Id { get; private set; }
    public Guid PetId { get; private set; }
    public Guid OwnerId { get; private set; }
    public Guid VeterinarianId { get; private set; }
    public Guid? AppointmentId { get; private set; }
    public DateTimeOffset ExaminedAtUtc { get; private set; }
    public Diagnosis Diagnosis { get; private set; } = null!;
    public TreatmentPlan TreatmentPlan { get; private set; } = null!;
    public List<string> Medications { get; private set; } = [];
    public DateTimeOffset? NextControlAtUtc { get; private set; }
    public string? Notes { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    [NotMapped]
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    private MedicalExamination() { }

    public MedicalExamination(Guid petId, Guid ownerId, Guid veterinarianId, Guid? appointmentId,
        DateTimeOffset examinedAtUtc, Diagnosis diagnosis, TreatmentPlan treatmentPlan,
        IEnumerable<string>? medications, DateTimeOffset? nextControlAtUtc, string? notes)
    {
        if (petId == Guid.Empty || ownerId == Guid.Empty || veterinarianId == Guid.Empty)
            throw new DomainValidationException("Pet, owner and veterinarian are required.");
        if (examinedAtUtc == default)
            throw new DomainValidationException("Examination time is required.");
        if (nextControlAtUtc.HasValue && nextControlAtUtc <= examinedAtUtc)
            throw new DomainValidationException("Follow-up must be after the examination.");

        Id = Guid.NewGuid();
        PetId = petId;
        OwnerId = ownerId;
        VeterinarianId = veterinarianId;
        AppointmentId = appointmentId;
        ExaminedAtUtc = examinedAtUtc.ToUniversalTime();
        Diagnosis = diagnosis ?? throw new ArgumentNullException(nameof(diagnosis));
        TreatmentPlan = treatmentPlan ?? throw new ArgumentNullException(nameof(treatmentPlan));
        Medications = Normalize(medications);
        NextControlAtUtc = nextControlAtUtc?.ToUniversalTime();
        Notes = NormalizeOptional(notes, 2000, "Notes");
        CreatedAtUtc = DateTimeOffset.UtcNow;

        if (NextControlAtUtc.HasValue)
            _domainEvents.Add(new FollowUpReminderRequested(Id, OwnerId, PetId, NextControlAtUtc.Value, CreatedAtUtc));
    }

    public IReadOnlyCollection<IDomainEvent> DequeueDomainEvents()
    {
        var events = _domainEvents.ToArray();
        _domainEvents.Clear();
        return events;
    }

    private static List<string> Normalize(IEnumerable<string>? values) => values?
        .Where(value => !string.IsNullOrWhiteSpace(value))
        .Select(value => value.Trim())
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .ToList() ?? [];

    private static string? NormalizeOptional(string? value, int maximumLength, string fieldName)
    {
        var normalized = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        if (normalized?.Length > maximumLength)
            throw new DomainValidationException($"{fieldName} cannot exceed {maximumLength} characters.");
        return normalized;
    }
}
