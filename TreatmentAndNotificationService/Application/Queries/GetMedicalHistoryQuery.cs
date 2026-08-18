namespace TreatmentAndNotificationService.Application.Queries;

public sealed record GetMedicalHistoryQuery(Guid PetId);
public sealed record GetVeterinarianMedicalHistoryQuery(Guid VeterinarianId);
