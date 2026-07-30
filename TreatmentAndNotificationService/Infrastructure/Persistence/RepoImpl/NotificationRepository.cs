using Microsoft.EntityFrameworkCore;
using TreatmentAndNotificationService.Domain.Entities;
using TreatmentAndNotificationService.Domain.Enums;

namespace TreatmentAndNotificationService.Infrastructure.Persistence.RepoImpl;

public class NotificationRepository : INotificationRepository
{
    private readonly TreatmentDbContext _context;

    // ReSharper disable once ConvertToPrimaryConstructor
    public NotificationRepository(TreatmentDbContext context)
    {
        _context = context;
    }

    public Task AddNotification(Notification notification, CancellationToken cancellationToken)
    {
        return _context.Notifications.AddAsync(notification, cancellationToken).AsTask();
    }

    public Task<bool> SourceExists(string sourceEventId, CancellationToken cancellationToken)
    {
        return _context.Notifications.AnyAsync(item => item.SourceEventId == sourceEventId, cancellationToken);
    }

    public Task<List<Notification>> GetByOwnerId(Guid ownerId, CancellationToken cancellationToken)
    {
        return _context.Notifications
            .AsNoTracking()
            .Where(item => item.OwnerId == ownerId)
            .OrderByDescending(item => item.CreatedAtUtc)
            .ToListAsync(cancellationToken);
    }

    public Task<List<Notification>> GetDuePending(DateTimeOffset nowUtc, CancellationToken cancellationToken)
    {
        return _context.Notifications
            .Where(item => item.Status == NotificationStatus.Pending && item.ScheduledForUtc <= nowUtc)
            .OrderBy(item => item.ScheduledForUtc)
            .Take(100)
            .ToListAsync(cancellationToken);
    }
}