using AppointmentService.Application.Dtos;
using AppointmentService.Application.Queries;
using Microsoft.AspNetCore.Mvc;

namespace AppointmentService.Api.Controllers;

[ApiController]
[Route("veterinarians")]
public sealed class VeterinariansController(SearchVeterinariansHandler handler) : ControllerBase
{
    /// <summary>GET /veterinarians?clinicId=&amp;specialization= — both filters are optional.</summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<VeterinarianDto>>> Search(
        [FromQuery] Guid? clinicId, [FromQuery] string? specialization, CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new SearchVeterinariansQuery(clinicId, specialization), cancellationToken);
        return Ok(result);
    }
}
