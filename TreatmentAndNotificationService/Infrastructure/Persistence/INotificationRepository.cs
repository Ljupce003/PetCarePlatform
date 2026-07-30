using TreatmentAndNotificationService.Domain.Entities;

namespace TreatmentAndNotificationService.Infrastructure.Persistence;

public interface INotificationRepository
{
    Task AddNotification(Notification notification, CancellationToken cancellationToken);
    Task<bool> SourceExists(string sourceEventId, CancellationToken cancellationToken);
    Task<List<Notification>> GetByOwnerId(Guid ownerId, CancellationToken cancellationToken);
    Task<List<Notification>> GetDuePending(DateTimeOffset nowUtc, CancellationToken cancellationToken);
}