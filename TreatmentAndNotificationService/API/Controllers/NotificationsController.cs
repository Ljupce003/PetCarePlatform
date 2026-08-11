using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TreatmentAndNotificationService.Application.Abstractions;
using TreatmentAndNotificationService.Application.Commands;
using TreatmentAndNotificationService.Application.Models;
using TreatmentAndNotificationService.Application.Queries;

namespace TreatmentAndNotificationService.API.Controllers;

[ApiController]
[Route("api/notifications")]
[Authorize]
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

    /// <summary>Gets all notifications created for an owner, newest notification first.</summary>
    /// <param name="ownerId">The owner whose notifications should be returned.</param>
    /// <param name="ct">Request-abort cancellation token.</param>
    /// <returns>The owner's notification history; an owner without notifications receives an empty list.</returns>
    /// <response code="200">The notification-history query completed successfully.</response>
    /// <response code="404">The route does not contain a valid GUID owner identifier.</response>
    [HttpGet("owner/{ownerId:guid}")]
    [Authorize(Roles = "owner,admin,service")]
    [ProducesResponseType(typeof(IReadOnlyList<NotificationDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<IReadOnlyList<NotificationDto>> GetByOwner([FromRoute] Guid ownerId, CancellationToken ct) =>
        _notifications.HandleAsync(new GetOwnerNotificationsQuery(ownerId), ct);

    /// <summary>Creates a pending notification from a source event identifier.</summary>
    /// <param name="command">Owner, pet, notification type/content, schedule, and unique source event identifier.</param>
    /// <param name="ct">Request-abort cancellation token.</param>
    /// <returns>The newly created pending notification.</returns>
    /// <response code="201">The notification was created.</response>
    /// <response code="400">The notification content, schedule, identifiers, or source event identifier is invalid.</response>
    /// <response code="409">A notification with the same source event identifier already exists.</response>
    [HttpPost]
    [Authorize(Roles = "admin,service")]
    [ProducesResponseType(typeof(NotificationDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<NotificationDto>> Create([FromBody] CreateNotificationCommand command, CancellationToken ct)
    {
        var result = await _createNotification.HandleAsync(command, ct);
        return Created($"/api/notifications/{result.Id}", result);
    }
}
