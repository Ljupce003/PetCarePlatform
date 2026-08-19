using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TreatmentAndNotificationService.Application.Abstractions;
using TreatmentAndNotificationService.Application.Commands;
using TreatmentAndNotificationService.Application.Models;
using TreatmentAndNotificationService.Application.Queries;
using TreatmentAndNotificationService.API.Security;
using TreatmentAndNotificationService.Application.Mappings;
using TreatmentAndNotificationService.Domain.Repositories;
using TreatmentAndNotificationService.Domain.ValueObjects;
using TreatmentAndNotificationService.Application.Services;
using TreatmentAndNotificationService.Domain.Enums;

namespace TreatmentAndNotificationService.API.Controllers;

[ApiController]
[Route("api/treatments")]
[Authorize]
public sealed class TreatmentsController: ControllerBase
{
    private readonly ICommandHandler<RecordMedicalExaminationCommand, MedicalExaminationDto> _recordExamination;
    private readonly IQueryHandler<GetMedicalHistoryQuery, IReadOnlyList<MedicalExaminationDto>> _medicalHistory;
    private readonly IQueryHandler<GetVeterinarianMedicalHistoryQuery, IReadOnlyList<MedicalExaminationDto>> _medicalHistoryByVeterinarian;
    private readonly IMedicalExaminationRepository _examinations;
    private readonly IUnitOfWork _unitOfWork;
    private readonly OwnerNotificationService _ownerNotifications;


    // ReSharper disable once ConvertToPrimaryConstructor
    public TreatmentsController(ICommandHandler<RecordMedicalExaminationCommand, MedicalExaminationDto> recordExamination,
        IQueryHandler<GetMedicalHistoryQuery, IReadOnlyList<MedicalExaminationDto>> medicalHistory,
        IQueryHandler<GetVeterinarianMedicalHistoryQuery, IReadOnlyList<MedicalExaminationDto>> medicalHistoryByVeterinarian,
        IMedicalExaminationRepository examinations, IUnitOfWork unitOfWork, OwnerNotificationService ownerNotifications)
    {
        _recordExamination = recordExamination;
        _medicalHistory = medicalHistory;
        _medicalHistoryByVeterinarian = medicalHistoryByVeterinarian;
        _examinations = examinations;
        _unitOfWork = unitOfWork;
        _ownerNotifications = ownerNotifications;
    }

    /// <summary>Gets the complete medical examination history for one pet, newest examination first.</summary>
    /// <param name="petId">The pet whose examination records should be returned.</param>
    /// <param name="ownerId">Required for an owner request; used to scope records to that owner.</param>
    /// <param name="ct">Request-abort cancellation token.</param>
    /// <returns>A list of medical examinations. An unknown pet returns an empty list until Pet Service validation is introduced.</returns>
    /// <response code="200">The medical-history query completed successfully.</response>
    /// <response code="404">The route does not contain a valid GUID pet identifier.</response>
    [HttpGet("pet/{petId:guid}")]
    [ProducesResponseType(typeof(IReadOnlyList<MedicalExaminationDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<MedicalExaminationDto>>> History(
        [FromRoute] Guid petId, [FromQuery] Guid? ownerId, CancellationToken ct)
    {
        if (User.IsInRole("owner") && !User.IsInRole("admin"))
        {
            if (ownerId is not { } id || !UserOwnership.CanAccessOwner(User, id)) return Forbid();
            return Ok((await _medicalHistory.HandleAsync(new GetMedicalHistoryQuery(petId), ct))
                .Where(examination => examination.OwnerId == id).ToList());
        }
        return Ok(await _medicalHistory.HandleAsync(new GetMedicalHistoryQuery(petId), ct));
    }

    [HttpGet("veterinarian/{veterinarianId:guid}")]
    [Authorize(Roles = "veterinarian,admin")]
    public async Task<ActionResult<IReadOnlyList<MedicalExaminationDto>>> VeterinarianHistory(
        [FromRoute] Guid veterinarianId, CancellationToken ct)
    {
        if (!UserOwnership.CanAccessVeterinarian(User, veterinarianId)) return Forbid();
        return Ok(await _medicalHistoryByVeterinarian.HandleAsync(new GetVeterinarianMedicalHistoryQuery(veterinarianId), ct));
    }

    /// <summary>Records a completed medical examination and schedules a follow-up reminder when one is requested.</summary>
    /// <param name="request">Pet, owner, veterinarian, clinical findings, treatment plan, medications, and optional follow-up details.</param>
    /// <param name="ct">Request-abort cancellation token.</param>
    /// <returns>The newly recorded examination, including its generated identifier.</returns>
    /// <response code="201">The examination and any follow-up reminder were recorded.</response>
    /// <response code="400">A required identifier or medical field is missing or invalid; follow-up must be after the examination.</response>
    [HttpPost]
    [Authorize(Roles = "veterinarian,admin")]
    [ProducesResponseType(typeof(MedicalExaminationDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<MedicalExaminationDto>> Record([FromBody] RecordMedicalExaminationRequest request, CancellationToken ct)
    {
        if (!UserOwnership.CanAccessVeterinarian(User, request.VeterinarianId)) return Forbid();
        var result = await _recordExamination.HandleAsync(new RecordMedicalExaminationCommand(request.PetId, request.OwnerId,
            request.VeterinarianId, request.AppointmentId, request.ExaminedAtUtc, request.Diagnosis, request.TreatmentPlan,
            request.Medications, request.NextControlAtUtc, request.Notes), ct);
        return Created($"/api/treatments/pet/{result.PetId}", result);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "veterinarian,admin")]
    public async Task<ActionResult<MedicalExaminationDto>> Update([FromRoute] Guid id, [FromBody] RecordMedicalExaminationRequest request, CancellationToken ct)
    {
        var examination = await _examinations.GetByIdAsync(id, ct);
        if (examination is null) return NotFound();
        if (!UserOwnership.CanAccessVeterinarian(User, examination.VeterinarianId)) return Forbid();
        examination.Update(request.ExaminedAtUtc, Diagnosis.Create(request.Diagnosis), TreatmentPlan.Create(request.TreatmentPlan), request.Medications, request.NextControlAtUtc, request.Notes);
        await _ownerNotifications.AddAsync(examination.OwnerId, examination.PetId, NotificationType.MedicalRecordUpdated,
            "Medical examination updated", "A medical examination in your pet's care record was updated.",
            $"medical-examination:{examination.Id}:updated:{Guid.NewGuid():N}", ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Ok(examination.ToDto());
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "veterinarian,admin")]
    public async Task<IActionResult> Delete([FromRoute] Guid id, CancellationToken ct)
    {
        var examination = await _examinations.GetByIdAsync(id, ct);
        if (examination is null) return NotFound();
        if (!UserOwnership.CanAccessVeterinarian(User, examination.VeterinarianId)) return Forbid();
        await _ownerNotifications.AddAsync(examination.OwnerId, examination.PetId, NotificationType.MedicalRecordDeleted,
            "Medical examination removed", "A medical examination was removed from your pet's care record.",
            $"medical-examination:{examination.Id}:deleted:{Guid.NewGuid():N}", ct);
        _examinations.Remove(examination);
        await _unitOfWork.SaveChangesAsync(ct);
        return NoContent();
    }
}
