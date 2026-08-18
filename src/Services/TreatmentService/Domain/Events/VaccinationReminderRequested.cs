namespace TreatmentAndNotificationService.Domain.Events;

public sealed record VaccinationReminderRequested(
    Guid VaccinationId,
    Guid OwnerId,
    Guid PetId,
    string VaccineName,
    DateOnly DueOn,
    DateTimeOffset OccurredOnUtc) : IDomainEvent;
