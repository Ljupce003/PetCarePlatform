namespace TreatmentAndNotificationService.Application.Commands;

public sealed record RecordVaccinationCommand(
    Guid PetId, Guid OwnerId, Guid VeterinarianId, string? VaccineName,
    DateOnly AdministeredOn, DateOnly? NextDueOn, string? BatchNumber);
