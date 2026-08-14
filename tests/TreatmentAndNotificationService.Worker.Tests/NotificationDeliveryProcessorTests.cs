using Microsoft.Extensions.Logging.Abstractions;
using TreatmentAndNotificationService.Domain.Entities;
using TreatmentAndNotificationService.Domain.Enums;
using TreatmentAndNotificationService.Domain.Repositories;
using TreatmentAndNotificationService.Domain.ValueObjects;
using TreatmentAndNotificationService.Infrastructure.Notifications;

namespace TreatmentAndNotificationService.Worker.Tests;

public sealed class NotificationDeliveryProcessorTests
{
    private static readonly DateTimeOffset Now =
        new(2026, 8, 14, 10, 30, 0, TimeSpan.Zero);

    [Fact]
    public async Task DeliverDueAsync_WhenNothingIsDue_DoesNotSendOrSave()
    {
        var repository = new NotificationRepositoryFake([]);
        var sender = new NotificationSenderFake();
        var unitOfWork = new UnitOfWorkFake();
        var processor = CreateProcessor(repository, unitOfWork, sender);

        var processed = await processor.DeliverDueAsync(100, CancellationToken.None);

        Assert.Equal(0, processed);
        Assert.Empty(sender.Delivered);
        Assert.Equal(0, unitOfWork.SaveCalls);
        Assert.Equal(Now, repository.RequestedAtUtc);
        Assert.Equal(100, repository.RequestedBatchSize);
    }

    [Fact]
    public async Task DeliverDueAsync_WhenDeliverySucceeds_MarksSentAndSavesOnce()
    {
        var notification = CreateNotification("event:success");
        var repository = new NotificationRepositoryFake([notification]);
        var sender = new NotificationSenderFake();
        var unitOfWork = new UnitOfWorkFake();
        var processor = CreateProcessor(repository, unitOfWork, sender);

        var processed = await processor.DeliverDueAsync(100, CancellationToken.None);

        Assert.Equal(1, processed);
        Assert.Equal([notification], sender.Delivered);
        Assert.Equal(NotificationStatus.Sent, notification.Status);
        Assert.Equal(Now, notification.SentAtUtc);
        Assert.Null(notification.FailureReason);
        Assert.Equal(1, unitOfWork.SaveCalls);
    }

    [Fact]
    public async Task DeliverDueAsync_WhenOneDeliveryFails_MarksItFailedAndContinuesBatch()
    {
        var failed = CreateNotification("event:failed");
        var sent = CreateNotification("event:sent");
        var repository = new NotificationRepositoryFake([failed, sent]);
        var sender = new NotificationSenderFake(failed.Id);
        var unitOfWork = new UnitOfWorkFake();
        var processor = CreateProcessor(repository, unitOfWork, sender);

        var processed = await processor.DeliverDueAsync(100, CancellationToken.None);

        Assert.Equal(2, processed);
        Assert.Equal(NotificationStatus.Failed, failed.Status);
        Assert.Equal("Simulated delivery failure.", failed.FailureReason);
        Assert.Equal(NotificationStatus.Sent, sent.Status);
        Assert.Equal(2, sender.Attempts);
        Assert.Equal(1, unitOfWork.SaveCalls);
    }

    [Fact]
    public async Task DeliverDueAsync_WhenDeliveryIsCancelled_PropagatesAndDoesNotSave()
    {
        var notification = CreateNotification("event:cancelled");
        var repository = new NotificationRepositoryFake([notification]);
        var sender = new CancellingNotificationSender();
        var unitOfWork = new UnitOfWorkFake();
        var processor = CreateProcessor(repository, unitOfWork, sender);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            processor.DeliverDueAsync(100, new CancellationToken(canceled: true)));

        Assert.Equal(NotificationStatus.Pending, notification.Status);
        Assert.Equal(0, unitOfWork.SaveCalls);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task DeliverDueAsync_WhenBatchSizeIsInvalid_RejectsIt(int batchSize)
    {
        var processor = CreateProcessor(
            new NotificationRepositoryFake([]),
            new UnitOfWorkFake(),
            new NotificationSenderFake());

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            processor.DeliverDueAsync(batchSize, CancellationToken.None));
    }

    private static NotificationDeliveryProcessor CreateProcessor(
        INotificationRepository repository,
        IUnitOfWork unitOfWork,
        INotificationSender sender) =>
        new(repository, unitOfWork, sender, new FixedTimeProvider(Now),
            NullLogger<NotificationDeliveryProcessor>.Instance);

    private static Notification CreateNotification(string sourceEventId) =>
        new(
            Guid.NewGuid(),
            Guid.NewGuid(),
            NotificationType.AppointmentScheduled,
            NotificationContent.Create("Appointment reminder", "Your appointment is tomorrow."),
            Now.AddMinutes(-1),
            SourceEventId.Create(sourceEventId));

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }

    private sealed class NotificationRepositoryFake(IReadOnlyList<Notification> due)
        : INotificationRepository
    {
        public DateTimeOffset? RequestedAtUtc { get; private set; }
        public int? RequestedBatchSize { get; private set; }

        public Task<IReadOnlyList<Notification>> GetDuePendingAsync(
            DateTimeOffset nowUtc, int take, CancellationToken cancellationToken)
        {
            RequestedAtUtc = nowUtc;
            RequestedBatchSize = take;
            return Task.FromResult(due);
        }

        public Task AddAsync(Notification notification, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<bool> ExistsBySourceEventIdAsync(string sourceEventId, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<Notification>> GetByOwnerIdAsync(Guid ownerId, CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }

    private sealed class UnitOfWorkFake : IUnitOfWork
    {
        public int SaveCalls { get; private set; }

        public Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            SaveCalls++;
            return Task.CompletedTask;
        }
    }

    private sealed class NotificationSenderFake(Guid? failingNotificationId = null) : INotificationSender
    {
        public List<Notification> Delivered { get; } = [];
        public int Attempts { get; private set; }

        public Task SendAsync(Notification notification, CancellationToken cancellationToken)
        {
            Attempts++;
            if (notification.Id == failingNotificationId)
                throw new InvalidOperationException("Simulated delivery failure.");

            Delivered.Add(notification);
            return Task.CompletedTask;
        }
    }

    private sealed class CancellingNotificationSender : INotificationSender
    {
        public Task SendAsync(Notification notification, CancellationToken cancellationToken) =>
            Task.FromCanceled(cancellationToken);
    }
}
