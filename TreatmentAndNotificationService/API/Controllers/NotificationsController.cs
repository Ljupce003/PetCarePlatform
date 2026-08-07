using Microsoft.AspNetCore.Mvc;
using TreatmentAndNotificationService.Application.Abstractions;
using TreatmentAndNotificationService.Application.Commands;
using TreatmentAndNotificationService.Application.Models;
using TreatmentAndNotificationService.Application.Queries;

namespace TreatmentAndNotificationService.API.Controllers;

[ApiController]
[Route("api/notifications")]
public class NotificationsController : ControllerBase
{
    private readonly ICommandHandler<CreateNotificationCommand, NotificationDto> _createNotification;
    private readonly IQueryHandler<GetOwnerNotificationsQuery, IReadOnlyList<NotificationDto>> _notifications;

    // ReSharper disable once ConvertToPrimaryConstructor
    public NotificationsController(ICommandHandler<CreateNotificationCommand, NotificationDto> createNotification, IQueryHandler<GetOwnerNotificationsQuery, IReadOnlyList<NotificationDto>> notifications)
    {
        _createNotification = createNotification;
        _notifications = notifications;
    }

    [HttpGet("owner/{ownerId:guid}")]
    public Task<IReadOnlyList<NotificationDto>> GetByOwner(Guid ownerId, CancellationToken ct) =>
        _notifications.HandleAsync(new GetOwnerNotificationsQuery(ownerId), ct);

    [HttpPost]
    public async Task<ActionResult<NotificationDto>> Create(CreateNotificationCommand command, CancellationToken ct)
    {
        var result = await _createNotification.HandleAsync(command, ct);
        return Created($"/api/notifications/{result.Id}", result);
    }
}
