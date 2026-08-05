using AppointmentService.Application.Dtos;
using AppointmentService.Application.Queries;
using Microsoft.AspNetCore.Mvc;

namespace AppointmentService.Api.Controllers;

[ApiController]
[Route("api/clinics")]
public sealed class ClinicsController(SearchClinicsHandler handler) : ControllerBase
{
    /// <summary>GET /api/clinics?location=Skopje — location filter is optional.</summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ClinicDto>>> Search(
        [FromQuery] string? location, CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new SearchClinicsQuery(location), cancellationToken);
        return Ok(result);
    }
}
