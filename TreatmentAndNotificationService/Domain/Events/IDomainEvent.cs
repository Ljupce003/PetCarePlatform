namespace TreatmentAndNotificationService.Domain.Events;

public interface IDomainEvent
{
    DateTimeOffset OccurredOnUtc { get; }
}
