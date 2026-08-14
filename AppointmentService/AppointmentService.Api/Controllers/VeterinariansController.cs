using AppointmentService.Application.Dtos;
using AppointmentService.Application.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AppointmentService.Api.Controllers;

[ApiController]
[Route("veterinarians")]
[Authorize]
public sealed class VeterinariansController(
    SearchVeterinariansHandler searchHandler,
    FindAvailableVeterinariansHandler findAvailableHandler) : ControllerBase
{
    /// <summary>GET /veterinarians?clinicId=&amp;specialization= — both filters are optional.</summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<VeterinarianDto>>> Search(
        [FromQuery] Guid? clinicId, [FromQuery] string? specialization, CancellationToken cancellationToken)
    {
        var result = await searchHandler.HandleAsync(new SearchVeterinariansQuery(clinicId, specialization), cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// GET /veterinarians/available?date=2026-08-18&amp;location=Skopje&amp;specialization=Surgery —
    /// veterinarians with at least one open slot on the given date, each with their open slots on
    /// that date attached. <c>date</c> is required; <c>location</c> and <c>specialization</c> are
    /// optional filters. This is the endpoint the shared MCP server's "find_open_appointment_slots"
    /// tool calls (see FindAvailableVeterinariansHandler for why it composes clinics + slots
    /// instead of a dedicated repository query).
    /// </summary>
    [HttpGet("available")]
    public async Task<ActionResult<IReadOnlyList<AvailableVeterinarianDto>>> Available(
        [FromQuery] DateOnly date, [FromQuery] string? location, [FromQuery] string? specialization,
        CancellationToken cancellationToken)
    {
        var result = await findAvailableHandler.HandleAsync(
            new FindAvailableVeterinariansQuery(date, location, specialization), cancellationToken);
        return Ok(result);
    }
}
