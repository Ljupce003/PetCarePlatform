using AppointmentService.Application.Commands;
using AppointmentService.Application.Dtos;
using AppointmentService.Application.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AppointmentService.Api.Controllers;

[ApiController]
[Route("appointments")]
[Authorize]
public sealed class AppointmentsController(
    ScheduleAppointmentHandler scheduleHandler,
    CancelAppointmentHandler cancelHandler,
    RescheduleAppointmentHandler rescheduleHandler,
    GetUpcomingAppointmentsHandler upcomingHandler) : ControllerBase
{
    /// <summary>GET /appointments/upcoming?ownerId=... — upcoming (still-scheduled) appointments for an owner.</summary>
    [HttpGet("upcoming")]
    public async Task<ActionResult<IReadOnlyList<AppointmentDto>>> GetUpcoming(
        [FromQuery] Guid ownerId, CancellationToken cancellationToken)
    {
        var result = await upcomingHandler.HandleAsync(new GetUpcomingAppointmentsQuery(ownerId), cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// POST /appointments — books a new appointment. Verifies pet ownership with the Pet
    /// Service and reserves the requested availability slot; fails with 404/403/409 if the pet,
    /// slot, or slot state doesn't check out (see the global exception mapping in Program.cs).
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "owner,admin")]
    public async Task<ActionResult<AppointmentDto>> Schedule(
        ScheduleAppointmentCommand command, CancellationToken cancellationToken)
    {
        var result = await scheduleHandler.HandleAsync(command, cancellationToken);
        return Created($"/appointments/{result.AppointmentId}", result);
    }

    /// <summary>
    /// DELETE /appointments/{id}?reason=... — cancels a still-scheduled appointment and frees its
    /// slot. Reason is optional and passed as a query parameter since DELETE requests don't carry
    /// a conventional body.
    /// </summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "owner,admin")]
    public async Task<ActionResult<AppointmentDto>> Cancel(
        Guid id, [FromQuery] string? reason, CancellationToken cancellationToken)
    {
        var result = await cancelHandler.HandleAsync(new CancelAppointmentCommand(id, reason), cancellationToken);
        return Ok(result);
    }

    /// <summary>PUT /appointments/{id}/reschedule — moves a still-scheduled appointment onto a different open slot.</summary>
    [HttpPut("{id:guid}/reschedule")]
    [Authorize(Roles = "owner,admin")]
    public async Task<ActionResult<AppointmentDto>> Reschedule(
        Guid id, RescheduleAppointmentRequest request, CancellationToken cancellationToken)
    {
        var result = await rescheduleHandler.HandleAsync(
            new RescheduleAppointmentCommand(id, request.NewAvailabilitySlotId), cancellationToken);
        return Ok(result);
    }
}

public sealed record RescheduleAppointmentRequest(Guid NewAvailabilitySlotId);
