namespace TreatmentAndNotificationService.Application.Queries;

public sealed record GetVaccinationHistoryQuery(Guid PetId);
public sealed record GetVeterinarianVaccinationHistoryQuery(Guid VeterinarianId);
