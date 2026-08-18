using AppointmentService.Application.Commands;
using AppointmentService.Application.Abstractions;
using AppointmentService.Application.Dtos;
using AppointmentService.Application.Queries;
using AppointmentService.Api.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AppointmentService.Api.Controllers;

[ApiController]
[Route("slots")]
[Authorize]
public sealed class AvailabilitySlotsController(
    SearchAvailableSlotsHandler searchHandler,
    CreateAvailabilitySlotHandler createHandler,
    UpdateAvailabilitySlotHandler updateHandler,
    DeleteAvailabilitySlotHandler deleteHandler,
    IAvailabilitySlotRepository slots) : ControllerBase
{
    /// <summary>
    /// GET /slots?veterinarianId=&amp;date=2026-08-10 — both filters are optional.
    /// Only open (not yet booked, not in the past) slots are returned.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<AvailableSlotDto>>> Search(
        [FromQuery] Guid? veterinarianId, [FromQuery] DateOnly? date, CancellationToken cancellationToken)
    {
        var result = await searchHandler.HandleAsync(new SearchAvailableSlotsQuery(veterinarianId, date), cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// POST /slots — opens a new availability slot for an existing veterinarian. Not part of the
    /// original section 8 spec (which only ever reads slots); added so a clinic/admin actually has
    /// a way to open slots beyond what was seeded, instead of only through the database seeder.
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "veterinarian,admin")]
    public async Task<ActionResult<AvailableSlotDto>> Create(
        CreateAvailabilitySlotCommand command, CancellationToken cancellationToken)
    {
        if (!UserOwnership.CanAccessVeterinarian(User, command.VeterinarianId)) return Forbid();
        var result = await createHandler.HandleAsync(command, cancellationToken);
        return Created($"/slots/{result.AvailabilitySlotId}", result);
    }

    [HttpPut("{availabilitySlotId:guid}")]
    [Authorize(Roles = "veterinarian,admin")]
    public async Task<ActionResult<AvailableSlotDto>> Update([FromRoute] Guid availabilitySlotId,
        [FromBody] UpdateAvailabilitySlotCommand command, CancellationToken cancellationToken)
    {
        var slot = await slots.GetByIdAsync(availabilitySlotId, cancellationToken);
        if (slot is null) return NotFound();
        if (!UserOwnership.CanAccessVeterinarian(User, slot.VeterinarianId)) return Forbid();
        return Ok(await updateHandler.HandleAsync(command with { AvailabilitySlotId = availabilitySlotId }, cancellationToken));
    }

    [HttpDelete("{availabilitySlotId:guid}")]
    [Authorize(Roles = "veterinarian,admin")]
    public async Task<IActionResult> Delete([FromRoute] Guid availabilitySlotId, CancellationToken cancellationToken)
    {
        var slot = await slots.GetByIdAsync(availabilitySlotId, cancellationToken);
        if (slot is null) return NotFound();
        if (!UserOwnership.CanAccessVeterinarian(User, slot.VeterinarianId)) return Forbid();
        await deleteHandler.HandleAsync(availabilitySlotId, cancellationToken);
        return NoContent();
    }
}
