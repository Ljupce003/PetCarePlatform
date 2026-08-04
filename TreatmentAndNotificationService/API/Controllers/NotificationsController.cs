using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TreatmentAndNotificationService.Application.Models;
using TreatmentAndNotificationService.Application.Services;

namespace TreatmentAndNotificationService.API.Controllers;

[ApiController]
[Route("api/notifications")]
// [Authorize]
public class NotificationsController
{
    private readonly ITreatmentApplicationService _service;

    // ReSharper disable once ConvertToPrimaryConstructor
    public NotificationsController(ITreatmentApplicationService service)
    {
        _service = service;
    }

    [HttpGet("owner/{ownerId:guid}")]
    public Task<List<NotificationDto>> GetByOwner(Guid ownerId, CancellationToken ct)
    {
        return _service.GetNotificationsAsync(ownerId, ct);
    }
}