namespace TreatmentAndNotificationService.Domain.Events;

public sealed record FollowUpReminderRequested(
    Guid ExaminationId,
    Guid OwnerId,
    Guid PetId,
    DateTimeOffset FollowUpAtUtc,
    DateTimeOffset OccurredOnUtc) : IDomainEvent;
