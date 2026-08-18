using TreatmentAndNotificationService.Domain.Events;

namespace TreatmentAndNotificationService.Application.Events;

public sealed class DomainEventDispatcher(
    FollowUpReminderRequestedHandler followUpHandler,
    VaccinationReminderRequestedHandler vaccinationHandler) : IDomainEventDispatcher
{
    public async Task DispatchAsync(IEnumerable<IDomainEvent> events, CancellationToken cancellationToken)
    {
        foreach (var domainEvent in events)
        {
            switch (domainEvent)
            {
                case FollowUpReminderRequested followUp:
                    await followUpHandler.HandleAsync(followUp, cancellationToken);
                    break;
                case VaccinationReminderRequested vaccination:
                    await vaccinationHandler.HandleAsync(vaccination, cancellationToken);
                    break;
                default:
                    throw new InvalidOperationException($"No handler has been registered for {domainEvent.GetType().Name}.");
            }
        }
    }
}
