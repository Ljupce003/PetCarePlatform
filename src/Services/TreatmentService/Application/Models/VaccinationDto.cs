namespace TreatmentAndNotificationService.Application.Models;

public record VaccinationDto(
    Guid Id, Guid PetId, Guid OwnerId, Guid VeterinarianId,
    string VaccineName, DateOnly AdministeredOn, DateOnly? NextDueOn, string? BatchNumber);