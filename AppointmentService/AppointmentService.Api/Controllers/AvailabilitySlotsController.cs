using AppointmentService.Application.Dtos;
using AppointmentService.Application.Queries;
using Microsoft.AspNetCore.Mvc;

namespace AppointmentService.Api.Controllers;

[ApiController]
[Route("api/availability-slots")]
public sealed class AvailabilitySlotsController(SearchAvailableSlotsHandler handler) : ControllerBase
{
    /// <summary>
    /// GET /api/availability-slots?veterinarianId=&amp;date=2026-08-10 — both filters are optional.
    /// Only open (not yet booked, not in the past) slots are returned.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<AvailableSlotDto>>> Search(
        [FromQuery] Guid? veterinarianId, [FromQuery] DateOnly? date, CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new SearchAvailableSlotsQuery(veterinarianId, date), cancellationToken);
        return Ok(result);
    }
}
