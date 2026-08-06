using TreatmentAndNotificationService.Application.Abstractions;
using TreatmentAndNotificationService.Application.Mappings;
using TreatmentAndNotificationService.Application.Models;
using TreatmentAndNotificationService.Domain.Entities;
using TreatmentAndNotificationService.Domain.Common;
using TreatmentAndNotificationService.Domain.Repositories;
using TreatmentAndNotificationService.Domain.ValueObjects;

namespace TreatmentAndNotificationService.Application.Commands;

public sealed class CreateNotificationCommandHandler(INotificationRepository notifications, IUnitOfWork unitOfWork)
    : ICommandHandler<CreateNotificationCommand, NotificationDto>
{
    public async Task<NotificationDto> HandleAsync(CreateNotificationCommand command, CancellationToken cancellationToken)
    {
        if (await notifications.ExistsBySourceEventIdAsync(command.SourceEventId ?? string.Empty, cancellationToken))
            throw new DuplicateSourceEventException(command.SourceEventId!);

        var notification = new Notification(command.OwnerId, command.PetId, command.Type,
            NotificationContent.Create(command.Title, command.Message), command.ScheduledForUtc,
            SourceEventId.Create(command.SourceEventId));
        await notifications.AddAsync(notification, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return notification.ToDto();
    }
}
