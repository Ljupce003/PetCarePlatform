using TreatmentAndNotificationService.Domain.Entities;
using TreatmentAndNotificationService.Domain.Enums;
using TreatmentAndNotificationService.Domain.Repositories;
using TreatmentAndNotificationService.Domain.ValueObjects;

namespace TreatmentAndNotificationService.Application.Services;

/// <summary>Creates owner-targeted activity notifications inside the same unit of work as clinical changes.</summary>
public sealed class OwnerNotificationService(INotificationRepository notifications)
{
    public Task AddAsync(Guid ownerId, Guid petId, NotificationType type, string title, string message,
        string sourceEventId, CancellationToken cancellationToken) =>
        notifications.AddAsync(new Notification(ownerId, petId, type, NotificationContent.Create(title, message),
            DateTimeOffset.UtcNow, SourceEventId.Create(sourceEventId)), cancellationToken);
}
