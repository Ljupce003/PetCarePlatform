using TreatmentAndNotificationService.Application.Models;

namespace TreatmentAndNotificationService.Domain.Entities;

public class MedicalExamination
{
    public Guid Id { get; private set; }
    public Guid PetId { get; private set; }
    public Guid OwnerId { get; private set; }
    public Guid VeterinarianId { get; private set; }
    public Guid? AppointmentId { get; private set; }
    public DateTimeOffset ExaminedAtUtc { get; private set; }
    public string Diagnosis { get; private set; } = string.Empty;
    public string TreatmentPlan { get; private set; } = string.Empty;
    public List<string> Medications { get; private set; } = [];
    public DateTimeOffset? NextControlAtUtc { get; private set; }
    public string? Notes { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }

    private MedicalExamination()
    {
        
    }

    public MedicalExamination(Guid petId, Guid ownerId, Guid veterinarianId, Guid? appointmentId,
        DateTimeOffset examinedAtUtc, string diagnosis, string treatmentPlan,
        IEnumerable<string>? medications, DateTimeOffset? nextControlAtUtc, string? notes)
    {
        if (petId == Guid.Empty || ownerId == Guid.Empty || veterinarianId == Guid.Empty)
            throw new ArgumentException("Pet, owner and veterinarian are required.");
        
        if (string.IsNullOrWhiteSpace(diagnosis)) throw new ArgumentException("Diagnosis is required.");
        
        Id = Guid.NewGuid();
        PetId = petId;
        OwnerId = ownerId;
        VeterinarianId = veterinarianId;
        AppointmentId = appointmentId;
        ExaminedAtUtc = examinedAtUtc;
        Diagnosis = diagnosis.Trim();
        TreatmentPlan = treatmentPlan.Trim();
        Medications = medications?.Where(item => !string.IsNullOrWhiteSpace(item)).Select(item => item.Trim()).ToList() ?? [];
        NextControlAtUtc = nextControlAtUtc;
        Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
        CreatedAtUtc = DateTimeOffset.UtcNow;
    }
    
    public static MedicalExaminationDto ToDto(MedicalExamination item) => new(item.Id, item.PetId,
        item.OwnerId, item.VeterinarianId, item.AppointmentId, item.ExaminedAtUtc, item.Diagnosis,
        item.TreatmentPlan, item.Medications, item.NextControlAtUtc, item.Notes);
}