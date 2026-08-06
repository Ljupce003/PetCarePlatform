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

    [HttpGet("pet/{petId:guid}")]
    public Task<IReadOnlyList<MedicalExaminationDto>> History(Guid petId, CancellationToken ct) =>
        _medicalHistory.HandleAsync(new GetMedicalHistoryQuery(petId), ct);

    [HttpPost]
    public async Task<ActionResult<MedicalExaminationDto>> Record(RecordMedicalExaminationRequest request, CancellationToken ct)
    {
        var result = await _recordExamination.HandleAsync(new RecordMedicalExaminationCommand(request.PetId, request.OwnerId,
            request.VeterinarianId, request.AppointmentId, request.ExaminedAtUtc, request.Diagnosis, request.TreatmentPlan,
            request.Medications, request.NextControlAtUtc, request.Notes), ct);
        return Created($"/api/treatments/pet/{result.PetId}", result);
    }
}
