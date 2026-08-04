using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TreatmentAndNotificationService.Application.Models;
using TreatmentAndNotificationService.Application.Services;

namespace TreatmentAndNotificationService.API.Controllers;

[ApiController]
[Route("api/treatments")]
// [Authorize]
public class TreatmentsController : ControllerBase
{
    private readonly ITreatmentApplicationService _treatmentApplicationService;

    // ReSharper disable once ConvertToPrimaryConstructor
    public TreatmentsController(ITreatmentApplicationService treatmentApplicationService)
    {
        _treatmentApplicationService = treatmentApplicationService;
    }

    [HttpGet("pet/{petId:guid}")]
    public Task<List<MedicalExaminationDto>> History(Guid petId, CancellationToken ct)
    {
        return _treatmentApplicationService.GetMedicalHistory(petId, ct);
    }

    [HttpPost]
    [Authorize(Roles = "veterinarian,admin")]
    public async Task<ActionResult<MedicalExaminationDto>> Record(RecordMedicalExaminationRequest request,
        CancellationToken ct)
    {
        var result = await _treatmentApplicationService.RecordExaminationAsync(request, ct);
        return Created($"/api/treatments/pet/{result.PetId}", result);
    }
}