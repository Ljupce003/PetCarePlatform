using Microsoft.AspNetCore.Mvc;
using TreatmentAndNotificationService.Application.Abstractions;
using TreatmentAndNotificationService.Application.Commands;
using TreatmentAndNotificationService.Application.Models;
using TreatmentAndNotificationService.Application.Queries;

namespace TreatmentAndNotificationService.API.Controllers;

[ApiController]
[Route("api/treatments")]
public sealed class TreatmentsController: ControllerBase
{
    private readonly ICommandHandler<RecordMedicalExaminationCommand, MedicalExaminationDto> _recordExamination;
    private readonly IQueryHandler<GetMedicalHistoryQuery, IReadOnlyList<MedicalExaminationDto>> _medicalHistory;


    // ReSharper disable once ConvertToPrimaryConstructor
    public TreatmentsController(ICommandHandler<RecordMedicalExaminationCommand, MedicalExaminationDto> recordExamination, IQueryHandler<GetMedicalHistoryQuery, IReadOnlyList<MedicalExaminationDto>> medicalHistory)
    {
        _recordExamination = recordExamination;
        _medicalHistory = medicalHistory;
    }

    /// <summary>Gets the complete medical examination history for one pet, newest examination first.</summary>
    /// <param name="petId">The pet whose examination records should be returned.</param>
    /// <param name="ct">Request-abort cancellation token.</param>
    /// <returns>A list of medical examinations. An unknown pet returns an empty list until Pet Service validation is introduced.</returns>
    /// <response code="200">The medical-history query completed successfully.</response>
    /// <response code="404">The route does not contain a valid GUID pet identifier.</response>
    [HttpGet("pet/{petId:guid}")]
    [ProducesResponseType(typeof(IReadOnlyList<MedicalExaminationDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<IReadOnlyList<MedicalExaminationDto>> History([FromRoute] Guid petId, CancellationToken ct) =>
        _medicalHistory.HandleAsync(new GetMedicalHistoryQuery(petId), ct);

    /// <summary>Records a completed medical examination and schedules a follow-up reminder when one is requested.</summary>
    /// <param name="request">Pet, owner, veterinarian, clinical findings, treatment plan, medications, and optional follow-up details.</param>
    /// <param name="ct">Request-abort cancellation token.</param>
    /// <returns>The newly recorded examination, including its generated identifier.</returns>
    /// <response code="201">The examination and any follow-up reminder were recorded.</response>
    /// <response code="400">A required identifier or medical field is missing or invalid; follow-up must be after the examination.</response>
    [HttpPost]
    [ProducesResponseType(typeof(MedicalExaminationDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<MedicalExaminationDto>> Record([FromBody] RecordMedicalExaminationRequest request, CancellationToken ct)
    {
        var result = await _recordExamination.HandleAsync(new RecordMedicalExaminationCommand(request.PetId, request.OwnerId,
            request.VeterinarianId, request.AppointmentId, request.ExaminedAtUtc, request.Diagnosis, request.TreatmentPlan,
            request.Medications, request.NextControlAtUtc, request.Notes), ct);
        return Created($"/api/treatments/pet/{result.PetId}", result);
    }
}
