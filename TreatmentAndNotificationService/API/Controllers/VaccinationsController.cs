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

    [HttpGet("pet/{petId:guid}")]
    public Task<IReadOnlyList<VaccinationDto>> GetByPet(Guid petId, CancellationToken ct) =>
        _history.HandleAsync(new GetVaccinationHistoryQuery(petId), ct);

    [HttpGet("pet/{petId:guid}/next")]
    public async Task<ActionResult<VaccinationDto>> GetNext(Guid petId, CancellationToken ct)
    {
        var result = await _nextVaccination.HandleAsync(new GetNextVaccinationQuery(petId), ct);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<VaccinationDto>> Record(RecordVaccinationRequest request, CancellationToken ct)
    {
        var result = await _recordVaccination.HandleAsync(new RecordVaccinationCommand(request.PetId, request.OwnerId,
            request.VeterinarianId, request.VaccineName, request.AdministeredOn, request.NextDueOn, request.BatchNumber), ct);
        return Created($"/api/vaccinations/pet/{result.PetId}", result);
    }
}
