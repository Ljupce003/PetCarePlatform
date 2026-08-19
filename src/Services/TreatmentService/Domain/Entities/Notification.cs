using TreatmentAndNotificationService.Domain.Common;
using TreatmentAndNotificationService.Domain.Enums;
using TreatmentAndNotificationService.Domain.ValueObjects;

namespace TreatmentAndNotificationService.Domain.Entities;

/// <summary>Aggregate root for an idempotent, scheduled notification.</summary>
public class Notification
{
    public Guid Id { get; private set; }
    public Guid OwnerId { get; private set; }
    public Guid? VeterinarianId { get; private set; }
    public Guid PetId { get; private set; }
    public NotificationType Type { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string Message { get; private set; } = string.Empty;
    public DateTimeOffset ScheduledForUtc { get; private set; }
    public SourceEventId SourceEventId { get; private set; } = null!;
    public NotificationStatus Status { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset? SentAtUtc { get; private set; }
    public string? FailureReason { get; private set; }

    private Notification() { }

    public Notification(Guid ownerId, Guid petId, NotificationType type, NotificationContent content,
        DateTimeOffset scheduledForUtc, SourceEventId sourceEventId, Guid? veterinarianId = null)
    {
        if (ownerId == Guid.Empty || petId == Guid.Empty)
            throw new DomainValidationException("Owner and pet are required.");
        if (!Enum.IsDefined(type))
            throw new DomainValidationException("A valid notification type is required.");

        Id = Guid.NewGuid();
        OwnerId = ownerId;
        VeterinarianId = veterinarianId == Guid.Empty ? throw new DomainValidationException("Veterinarian identifier is invalid.") : veterinarianId;
        PetId = petId;
        Type = type;
        ArgumentNullException.ThrowIfNull(content);
        Title = content.Title;
        Message = content.Message;
        ScheduledForUtc = scheduledForUtc == default
            ? throw new DomainValidationException("Notification schedule is required.")
            : scheduledForUtc.ToUniversalTime();
        SourceEventId = sourceEventId ?? throw new ArgumentNullException(nameof(sourceEventId));
        Status = NotificationStatus.Pending;
        CreatedAtUtc = DateTimeOffset.UtcNow;
    }

    public void MarkSent(DateTimeOffset sentAtUtc)
    {
        if (Status != NotificationStatus.Pending)
            throw new DomainValidationException("Only pending notifications can be sent.");
        Status = NotificationStatus.Sent;
        SentAtUtc = sentAtUtc.ToUniversalTime();
        FailureReason = null;
    }

    public void MarkFailed(string? reason)
    {
        if (Status != NotificationStatus.Pending)
            throw new DomainValidationException("Only pending notifications can fail.");
        Status = NotificationStatus.Failed;
        FailureReason = string.IsNullOrWhiteSpace(reason) ? "Delivery failed." : reason.Trim()[..Math.Min(reason.Trim().Length, 500)];
    }
}
