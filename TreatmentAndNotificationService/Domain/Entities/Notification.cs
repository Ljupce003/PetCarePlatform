using TreatmentAndNotificationService.Application.Models;
using TreatmentAndNotificationService.Domain.Enums;

namespace TreatmentAndNotificationService.Domain.Entities;

public class Notification
{
    public Guid Id { get; private set; }
    public Guid OwnerId { get; private set; }
    public Guid PetId { get; private set; }
    public NotificationType Type { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string Message { get; private set; } = string.Empty;
    public DateTimeOffset ScheduledForUtc { get; private set; }
    public string SourceEventId { get; private set; } = string.Empty;
    public NotificationStatus Status { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset? SentAtUtc { get; private set; }
    
    private Notification() { }

    public Notification(Guid ownerId, Guid petId, NotificationType type, string title, string message,
        DateTimeOffset scheduledForUtc, string sourceEventId)
    {
        if (ownerId == Guid.Empty || petId == Guid.Empty) throw new ArgumentException("Owner and pet are required.");
        if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(message))
            throw new ArgumentException("Notification title and message are required.");
        Id = Guid.NewGuid();
        OwnerId = ownerId;
        PetId = petId;
        Type = type;
        Title = title.Trim();
        Message = message.Trim();
        ScheduledForUtc = scheduledForUtc;
        SourceEventId = sourceEventId;
        Status = NotificationStatus.Pending;
        CreatedAtUtc = DateTimeOffset.UtcNow;
    }

    public void MarkSent()
    {
        Status = NotificationStatus.Sent;
        SentAtUtc = DateTimeOffset.UtcNow;
    }

    public void MarkFailed() => Status = NotificationStatus.Failed;
    
    public static NotificationDto ToDto(Notification item) => new NotificationDto(item.Id, item.OwnerId, item.PetId,
        item.Type, item.Title, item.Message, item.ScheduledForUtc, item.Status, item.CreatedAtUtc, item.SentAtUtc);
}