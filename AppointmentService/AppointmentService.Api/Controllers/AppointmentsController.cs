using AppointmentService.Application.Commands;
using AppointmentService.Application.Dtos;
using AppointmentService.Application.Queries;
using Microsoft.AspNetCore.Mvc;

namespace AppointmentService.Api.Controllers;

[ApiController]
[Route("api/appointments")]
public sealed class AppointmentsController(
    ScheduleAppointmentHandler scheduleHandler,
    CancelAppointmentHandler cancelHandler,
    RescheduleAppointmentHandler rescheduleHandler,
    GetUpcomingAppointmentsHandler upcomingHandler) : ControllerBase
{
    /// <summary>GET /api/appointments/upcoming?ownerId=... — upcoming (still-scheduled) appointments for an owner.</summary>
    [HttpGet("upcoming")]
    public async Task<ActionResult<IReadOnlyList<AppointmentDto>>> GetUpcoming(
        [FromQuery] Guid ownerId, CancellationToken cancellationToken)
    {
        var result = await upcomingHandler.HandleAsync(new GetUpcomingAppointmentsQuery(ownerId), cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// POST /api/appointments — books a new appointment. Verifies pet ownership with the Pet
    /// Service and reserves the requested availability slot; fails with 404/403/409 if the pet,
    /// slot, or slot state doesn't check out (see the global exception mapping in Program.cs).
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<AppointmentDto>> Schedule(
        ScheduleAppointmentCommand command, CancellationToken cancellationToken)
    {
        var result = await scheduleHandler.HandleAsync(command, cancellationToken);
        return Created($"/api/appointments/{result.AppointmentId}", result);
    }

    /// <summary>POST /api/appointments/{id}/cancel — only a still-scheduled appointment can be cancelled.</summary>
    [HttpPost("{id:guid}/cancel")]
    public async Task<ActionResult<AppointmentDto>> Cancel(
        Guid id, [FromBody] CancelAppointmentRequest? request, CancellationToken cancellationToken)
    {
        var result = await cancelHandler.HandleAsync(new CancelAppointmentCommand(id, request?.Reason), cancellationToken);
        return Ok(result);
    }

    /// <summary>POST /api/appointments/{id}/reschedule — moves a still-scheduled appointment onto a different open slot.</summary>
    [HttpPost("{id:guid}/reschedule")]
    public async Task<ActionResult<AppointmentDto>> Reschedule(
        Guid id, RescheduleAppointmentRequest request, CancellationToken cancellationToken)
    {
        var result = await rescheduleHandler.HandleAsync(
            new RescheduleAppointmentCommand(id, request.NewAvailabilitySlotId), cancellationToken);
        return Ok(result);
    }
}

public sealed record CancelAppointmentRequest(string? Reason);

public sealed record RescheduleAppointmentRequest(Guid NewAvailabilitySlotId);
