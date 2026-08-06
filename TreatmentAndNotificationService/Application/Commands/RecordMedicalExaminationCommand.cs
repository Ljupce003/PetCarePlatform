namespace TreatmentAndNotificationService.Application.Commands;

public sealed record RecordMedicalExaminationCommand(
    Guid PetId, Guid OwnerId, Guid VeterinarianId, Guid? AppointmentId,
    DateTimeOffset ExaminedAtUtc, string? Diagnosis, string? TreatmentPlan,
    IReadOnlyList<string>? Medications, DateTimeOffset? NextControlAtUtc, string? Notes);
