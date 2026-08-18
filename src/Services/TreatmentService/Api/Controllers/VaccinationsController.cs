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
[Route("api/vaccinations")]
[Authorize]
public sealed class VaccinationsController: ControllerBase
{
    private readonly ICommandHandler<RecordVaccinationCommand, VaccinationDto> _recordVaccination;
    private readonly IQueryHandler<GetVaccinationHistoryQuery, IReadOnlyList<VaccinationDto>> _history;
    private readonly IQueryHandler<GetVeterinarianVaccinationHistoryQuery, IReadOnlyList<VaccinationDto>> _historyByVeterinarian;
    private readonly IQueryHandler<GetNextVaccinationQuery, VaccinationDto?> _nextVaccination;
    private readonly IVaccinationRepository _vaccinations;
    private readonly IUnitOfWork _unitOfWork;
    private readonly OwnerNotificationService _ownerNotifications;

    // ReSharper disable once ConvertToPrimaryConstructor
    public VaccinationsController(ICommandHandler<RecordVaccinationCommand, VaccinationDto> recordVaccination,
        IQueryHandler<GetVaccinationHistoryQuery, IReadOnlyList<VaccinationDto>> history,
        IQueryHandler<GetVeterinarianVaccinationHistoryQuery, IReadOnlyList<VaccinationDto>> historyByVeterinarian,
        IQueryHandler<GetNextVaccinationQuery, VaccinationDto?> nextVaccination,
        IVaccinationRepository vaccinations, IUnitOfWork unitOfWork, OwnerNotificationService ownerNotifications)
    {
        _recordVaccination = recordVaccination;
        _history = history;
        _historyByVeterinarian = historyByVeterinarian;
        _nextVaccination = nextVaccination;
        _vaccinations = vaccinations;
        _unitOfWork = unitOfWork;
        _ownerNotifications = ownerNotifications;
    }

    /// <summary>Gets every vaccination recorded for one pet, newest administration first.</summary>
    /// <param name="petId">The pet whose vaccination history should be returned.</param>
    /// <param name="ownerId">Required for an owner request; used to scope records to that owner.</param>
    /// <param name="ct">Request-abort cancellation token.</param>
    /// <returns>The vaccination history; an unknown pet currently produces an empty list.</returns>
    /// <response code="200">The vaccination-history query completed successfully.</response>
    /// <response code="404">The route does not contain a valid GUID pet identifier.</response>
    [HttpGet("pet/{petId:guid}")]
    [ProducesResponseType(typeof(IReadOnlyList<VaccinationDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<VaccinationDto>>> GetByPet(
        [FromRoute] Guid petId, [FromQuery] Guid? ownerId, CancellationToken ct)
    {
        if (User.IsInRole("owner") && !User.IsInRole("admin"))
        {
            if (ownerId is not { } id || !UserOwnership.CanAccessOwner(User, id)) return Forbid();
            return Ok((await _history.HandleAsync(new GetVaccinationHistoryQuery(petId), ct))
                .Where(vaccination => vaccination.OwnerId == id).ToList());
        }
        return Ok(await _history.HandleAsync(new GetVaccinationHistoryQuery(petId), ct));
    }

    [HttpGet("veterinarian/{veterinarianId:guid}")]
    [Authorize(Roles = "veterinarian,admin")]
    public async Task<ActionResult<IReadOnlyList<VaccinationDto>>> GetByVeterinarian(
        [FromRoute] Guid veterinarianId, CancellationToken ct)
    {
        if (!UserOwnership.CanAccessVeterinarian(User, veterinarianId)) return Forbid();
        return Ok(await _historyByVeterinarian.HandleAsync(new GetVeterinarianVaccinationHistoryQuery(veterinarianId), ct));
    }

    /// <summary>Gets the nearest vaccination with a due date today or later.</summary>
    /// <param name="petId">The pet whose next vaccination is requested.</param>
    /// <param name="ownerId">Required for an owner request; used to scope the result to that owner.</param>
    /// <param name="ct">Request-abort cancellation token.</param>
    /// <returns>The next due vaccination.</returns>
    /// <response code="200">An upcoming vaccination exists.</response>
    /// <response code="404">The pet has no vaccination with an upcoming due date, or the route identifier is invalid.</response>
    [HttpGet("pet/{petId:guid}/next")]
    [ProducesResponseType(typeof(VaccinationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<VaccinationDto>> GetNext([FromRoute] Guid petId, [FromQuery] Guid? ownerId, CancellationToken ct)
    {
        var result = await _nextVaccination.HandleAsync(new GetNextVaccinationQuery(petId), ct);
        if (result is null) return NotFound();
        if (User.IsInRole("owner") && !User.IsInRole("admin") &&
            (ownerId is not { } id || !UserOwnership.CanAccessOwner(User, id) || result.OwnerId != id))
            return Forbid();
        return Ok(result);
    }

    /// <summary>Records an administered vaccination and schedules a reminder when a next due date is supplied.</summary>
    /// <param name="request">Pet, owner, veterinarian, vaccine, administration date, optional next due date, and batch number.</param>
    /// <param name="ct">Request-abort cancellation token.</param>
    /// <returns>The newly recorded vaccination.</returns>
    /// <response code="201">The vaccination and any reminder were recorded.</response>
    /// <response code="400">A required field is missing or the next due date is not after the administration date.</response>
    [HttpPost]
    [Authorize(Roles = "veterinarian,admin")]
    [ProducesResponseType(typeof(VaccinationDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<VaccinationDto>> Record([FromBody] RecordVaccinationRequest request, CancellationToken ct)
    {
        if (!UserOwnership.CanAccessVeterinarian(User, request.VeterinarianId)) return Forbid();
        var result = await _recordVaccination.HandleAsync(new RecordVaccinationCommand(request.PetId, request.OwnerId,
            request.VeterinarianId, request.VaccineName, request.AdministeredOn, request.NextDueOn, request.BatchNumber), ct);
        return Created($"/api/vaccinations/pet/{result.PetId}", result);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "veterinarian,admin")]
    public async Task<ActionResult<VaccinationDto>> Update([FromRoute] Guid id, [FromBody] RecordVaccinationRequest request, CancellationToken ct)
    {
        var vaccination = await _vaccinations.GetByIdAsync(id, ct);
        if (vaccination is null) return NotFound();
        if (!UserOwnership.CanAccessVeterinarian(User, vaccination.VeterinarianId)) return Forbid();
        vaccination.Update(VaccineName.Create(request.VaccineName), VaccinationSchedule.Create(request.AdministeredOn, request.NextDueOn), request.BatchNumber);
        await _ownerNotifications.AddAsync(vaccination.OwnerId, vaccination.PetId, NotificationType.VaccinationUpdated,
            "Vaccination updated", "A vaccination in your pet's record was updated.",
            $"vaccination:{vaccination.Id}:updated:{Guid.NewGuid():N}", ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Ok(vaccination.ToDto());
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "veterinarian,admin")]
    public async Task<IActionResult> Delete([FromRoute] Guid id, CancellationToken ct)
    {
        var vaccination = await _vaccinations.GetByIdAsync(id, ct);
        if (vaccination is null) return NotFound();
        if (!UserOwnership.CanAccessVeterinarian(User, vaccination.VeterinarianId)) return Forbid();
        await _ownerNotifications.AddAsync(vaccination.OwnerId, vaccination.PetId, NotificationType.VaccinationDeleted,
            "Vaccination removed", "A vaccination was removed from your pet's record.",
            $"vaccination:{vaccination.Id}:deleted:{Guid.NewGuid():N}", ct);
        _vaccinations.Remove(vaccination);
        await _unitOfWork.SaveChangesAsync(ct);
        return NoContent();
    }
}
