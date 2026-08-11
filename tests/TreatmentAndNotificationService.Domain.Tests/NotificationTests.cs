using TreatmentAndNotificationService.Domain.Common;
using TreatmentAndNotificationService.Domain.Entities;
using TreatmentAndNotificationService.Domain.Enums;
using TreatmentAndNotificationService.Domain.ValueObjects;

namespace TreatmentAndNotificationService.Domain.Tests;

public sealed class NotificationTests
{
    private static readonly Guid OwnerId = Guid.NewGuid();
    private static readonly Guid PetId = Guid.NewGuid();

    [Fact]
    public void Constructor_CreatesPendingNotification()
    {
        var scheduledFor = new DateTimeOffset(2026, 8, 11, 9, 0, 0, TimeSpan.FromHours(2));

        var notification = Create(scheduledFor: scheduledFor);

        Assert.NotEqual(Guid.Empty, notification.Id);
        Assert.Equal(OwnerId, notification.OwnerId);
        Assert.Equal(PetId, notification.PetId);
        Assert.Equal(NotificationType.FollowUpReminder, notification.Type);
        Assert.Equal("Reminder", notification.Title);
        Assert.Equal(scheduledFor.ToUniversalTime(), notification.ScheduledForUtc);
        Assert.Equal(NotificationStatus.Pending, notification.Status);
        Assert.Null(notification.SentAtUtc);
        Assert.Null(notification.FailureReason);
    }

    [Theory]
    [InlineData("owner")]
    [InlineData("pet")]
    public void Constructor_WhenRequiredIdentifierIsMissing_Throws(string missing)
    {
        var ownerId = missing == "owner" ? Guid.Empty : OwnerId;
        var petId = missing == "pet" ? Guid.Empty : PetId;

        Assert.Throws<DomainValidationException>(() => Create(ownerId, petId));
    }

    [Fact]
    public void Constructor_RejectsInvalidTypeScheduleAndNullValueObjects()
    {
        Assert.Throws<DomainValidationException>(() => Create(type: (NotificationType)999));
        Assert.Throws<DomainValidationException>(() => Create(scheduledFor: DateTimeOffset.MinValue));
        Assert.Throws<ArgumentNullException>(() => new Notification(
            OwnerId, PetId, NotificationType.FollowUpReminder, null!, DateTimeOffset.UtcNow,
            SourceEventId.Create("event")));
        Assert.Throws<ArgumentNullException>(() => new Notification(
            OwnerId, PetId, NotificationType.FollowUpReminder,
            NotificationContent.Create("Reminder", "Visit"), DateTimeOffset.UtcNow, null!));
    }

    [Fact]
    public void MarkSent_TransitionsPendingNotificationToSent()
    {
        var notification = Create();
        var sentAt = new DateTimeOffset(2026, 8, 10, 12, 0, 0, TimeSpan.FromHours(2));

        notification.MarkSent(sentAt);

        Assert.Equal(NotificationStatus.Sent, notification.Status);
        Assert.Equal(sentAt.ToUniversalTime(), notification.SentAtUtc);
        Assert.Null(notification.FailureReason);
    }

    [Fact]
    public void MarkFailed_TransitionsPendingNotificationAndNormalizesReason()
    {
        var notification = Create();

        notification.MarkFailed("  Provider unavailable  ");

        Assert.Equal(NotificationStatus.Failed, notification.Status);
        Assert.Equal("Provider unavailable", notification.FailureReason);
        Assert.Null(notification.SentAtUtc);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void MarkFailed_WhenReasonIsMissing_UsesDefault(string? reason)
    {
        var notification = Create();

        notification.MarkFailed(reason);

        Assert.Equal("Delivery failed.", notification.FailureReason);
    }

    [Fact]
    public void MarkFailed_TruncatesReasonTo500Characters()
    {
        var notification = Create();

        notification.MarkFailed(new string('x', 600));

        Assert.Equal(500, notification.FailureReason!.Length);
    }

    [Fact]
    public void SentNotification_CannotTransitionAgain()
    {
        var notification = Create();
        notification.MarkSent(DateTimeOffset.UtcNow);

        Assert.Throws<DomainValidationException>(() => notification.MarkSent(DateTimeOffset.UtcNow));
        Assert.Throws<DomainValidationException>(() => notification.MarkFailed("retry"));
    }

    [Fact]
    public void FailedNotification_CannotTransitionAgain()
    {
        var notification = Create();
        notification.MarkFailed("failure");

        Assert.Throws<DomainValidationException>(() => notification.MarkSent(DateTimeOffset.UtcNow));
        Assert.Throws<DomainValidationException>(() => notification.MarkFailed("retry"));
    }

    private static Notification Create(
        Guid? ownerId = null,
        Guid? petId = null,
        NotificationType type = NotificationType.FollowUpReminder,
        NotificationContent? content = null,
        DateTimeOffset? scheduledFor = null,
        SourceEventId? sourceEventId = null) =>
        new(
            ownerId ?? OwnerId,
            petId ?? PetId,
            type,
            content ?? NotificationContent.Create("Reminder", "Visit the clinic"),
            scheduledFor ?? DateTimeOffset.UtcNow.AddDays(1),
            sourceEventId ?? SourceEventId.Create("unit-test-event"));
}
