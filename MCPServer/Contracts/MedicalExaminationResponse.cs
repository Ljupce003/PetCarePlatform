namespace MCPServer.Contracts;

public record MedicalExaminationResponse(
    Guid Id,
    Guid PetId,
    Guid OwnerId,
    Guid VeterinarianId,
    Guid? AppointmentId,
    DateTimeOffset ExaminedAtUtc,
    string Diagnosis,
    string TreatmentPlan,
    IReadOnlyList<string> Medications,
    DateTimeOffset? NextControlAtUtc,
    string? Notes);
