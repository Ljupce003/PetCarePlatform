using Microsoft.AspNetCore.Mvc;
using TreatmentAndNotificationService.Application.Abstractions;
using TreatmentAndNotificationService.Application.Commands;
using TreatmentAndNotificationService.Application.Models;
using TreatmentAndNotificationService.Application.Queries;

namespace TreatmentAndNotificationService.API.Controllers;

[ApiController]
[Route("api/vaccinations")]
public sealed class VaccinationsController: ControllerBase
{
    private readonly ICommandHandler<RecordVaccinationCommand, VaccinationDto> _recordVaccination;
    private readonly IQueryHandler<GetVaccinationHistoryQuery, IReadOnlyList<VaccinationDto>> _history;
    private readonly IQueryHandler<GetNextVaccinationQuery, VaccinationDto?> _nextVaccination;

    // ReSharper disable once ConvertToPrimaryConstructor
    public VaccinationsController(ICommandHandler<RecordVaccinationCommand, VaccinationDto> recordVaccination, IQueryHandler<GetVaccinationHistoryQuery, IReadOnlyList<VaccinationDto>> history, IQueryHandler<GetNextVaccinationQuery, VaccinationDto?> nextVaccination)
    {
        _recordVaccination = recordVaccination;
        _history = history;
        _nextVaccination = nextVaccination;
    }

    /// <summary>Gets every vaccination recorded for one pet, newest administration first.</summary>
    /// <param name="petId">The pet whose vaccination history should be returned.</param>
    /// <param name="ct">Request-abort cancellation token.</param>
    /// <returns>The vaccination history; an unknown pet currently produces an empty list.</returns>
    /// <response code="200">The vaccination-history query completed successfully.</response>
    /// <response code="404">The route does not contain a valid GUID pet identifier.</response>
    [HttpGet("pet/{petId:guid}")]
    [ProducesResponseType(typeof(IReadOnlyList<VaccinationDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<IReadOnlyList<VaccinationDto>> GetByPet([FromRoute] Guid petId, CancellationToken ct) =>
        _history.HandleAsync(new GetVaccinationHistoryQuery(petId), ct);

    /// <summary>Gets the nearest vaccination with a due date today or later.</summary>
    /// <param name="petId">The pet whose next vaccination is requested.</param>
    /// <param name="ct">Request-abort cancellation token.</param>
    /// <returns>The next due vaccination.</returns>
    /// <response code="200">An upcoming vaccination exists.</response>
    /// <response code="404">The pet has no vaccination with an upcoming due date, or the route identifier is invalid.</response>
    [HttpGet("pet/{petId:guid}/next")]
    [ProducesResponseType(typeof(VaccinationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<VaccinationDto>> GetNext([FromRoute] Guid petId, CancellationToken ct)
    {
        var result = await _nextVaccination.HandleAsync(new GetNextVaccinationQuery(petId), ct);
        return result is null ? NotFound() : Ok(result);
    }

    /// <summary>Records an administered vaccination and schedules a reminder when a next due date is supplied.</summary>
    /// <param name="request">Pet, owner, veterinarian, vaccine, administration date, optional next due date, and batch number.</param>
    /// <param name="ct">Request-abort cancellation token.</param>
    /// <returns>The newly recorded vaccination.</returns>
    /// <response code="201">The vaccination and any reminder were recorded.</response>
    /// <response code="400">A required field is missing or the next due date is not after the administration date.</response>
    [HttpPost]
    [ProducesResponseType(typeof(VaccinationDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<VaccinationDto>> Record([FromBody] RecordVaccinationRequest request, CancellationToken ct)
    {
        var result = await _recordVaccination.HandleAsync(new RecordVaccinationCommand(request.PetId, request.OwnerId,
            request.VeterinarianId, request.VaccineName, request.AdministeredOn, request.NextDueOn, request.BatchNumber), ct);
        return Created($"/api/vaccinations/pet/{result.PetId}", result);
    }
}
