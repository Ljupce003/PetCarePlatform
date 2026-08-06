using TreatmentAndNotificationService.Domain.Entities;

namespace TreatmentAndNotificationService.Domain.Repositories;

public interface INotificationRepository
{
    Task AddAsync(Notification notification, CancellationToken cancellationToken);
    Task<bool> ExistsBySourceEventIdAsync(string sourceEventId, CancellationToken cancellationToken);
    Task<IReadOnlyList<Notification>> GetByOwnerIdAsync(Guid ownerId, CancellationToken cancellationToken);
    Task<IReadOnlyList<Notification>> GetDuePendingAsync(DateTimeOffset nowUtc, int take, CancellationToken cancellationToken);
}
