using Microsoft.EntityFrameworkCore;
using TreatmentAndNotificationService.Domain.Entities;
using TreatmentAndNotificationService.Domain.Enums;
using TreatmentAndNotificationService.Domain.Repositories;
using TreatmentAndNotificationService.Domain.ValueObjects;

namespace TreatmentAndNotificationService.Infrastructure.Persistence.RepoImpl;

public class NotificationRepository : INotificationRepository
{
    private readonly TreatmentDbContext _context;

    // ReSharper disable once ConvertToPrimaryConstructor
    public NotificationRepository(TreatmentDbContext context)
    {
        _context = context;
    }

    public Task AddAsync(Notification notification, CancellationToken cancellationToken)
    {
        return _context.Notifications.AddAsync(notification, cancellationToken).AsTask();
    }

    public Task<bool> ExistsBySourceEventIdAsync(string sourceEventId, CancellationToken cancellationToken)
    {
        var source = SourceEventId.Create(sourceEventId);
        return _context.Notifications.AnyAsync(item => item.SourceEventId == source, cancellationToken);
    }

    public async Task<IReadOnlyList<Notification>> GetByOwnerIdAsync(Guid ownerId, CancellationToken cancellationToken)
    {
        return await _context.Notifications
            .AsNoTracking()
            .Where(item => item.OwnerId == ownerId)
            .OrderByDescending(item => item.CreatedAtUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Notification>> GetDuePendingAsync(DateTimeOffset nowUtc, int take, CancellationToken cancellationToken)
    {
        return await _context.Notifications
            .Where(item => item.Status == NotificationStatus.Pending && item.ScheduledForUtc <= nowUtc)
            .OrderBy(item => item.ScheduledForUtc)
            .Take(take)
            .ToListAsync(cancellationToken);
    }
}
