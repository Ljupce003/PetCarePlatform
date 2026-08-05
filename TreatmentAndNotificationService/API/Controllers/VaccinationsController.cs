using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TreatmentAndNotificationService.Application.Models;
using TreatmentAndNotificationService.Application.Services;

namespace TreatmentAndNotificationService.API.Controllers;

[ApiController]
[Route("api/vaccinations")]
[Authorize]
public class VaccinationsController : ControllerBase
{
    private readonly ITreatmentApplicationService _service;

    // ReSharper disable once ConvertToPrimaryConstructor
    public VaccinationsController(ITreatmentApplicationService service)
    {
        _service = service;
    }

    [HttpGet("pet/{petId:guid}")]
    public Task<List<VaccinationDto>> GetByPet(Guid petId, CancellationToken ct)
    {
        return _service.GetVaccinationsAsync(petId, ct);
    }

    [HttpGet("pet/{petId:guid}/next")]
    public async Task<ActionResult<VaccinationDto>> GetNext(Guid petId, CancellationToken ct)
    {
        var result = await _service.GetNextVaccinationAsync(petId, ct);
        return result is null ? NotFound() : Ok(result);    
    }


    [HttpPost]
    [Authorize(Roles = "veterinarian,admin")]
    public async Task<ActionResult<VaccinationDto>> Record(RecordVaccinationRequest request, CancellationToken ct)
    {
        var result = await _service.RecordVaccinationAsync(request, ct);
        return Created($"/api/vaccinations/pet/{result.PetId}", result);
    }
}