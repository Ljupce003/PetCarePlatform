using TreatmentAndNotificationService.Domain.Events;

namespace TreatmentAndNotificationService.Application.Events;

public interface IDomainEventDispatcher
{
    Task DispatchAsync(IEnumerable<IDomainEvent> events, CancellationToken cancellationToken);
}
