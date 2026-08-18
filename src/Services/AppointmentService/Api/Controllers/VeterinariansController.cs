using AppointmentService.Application.Dtos;
using AppointmentService.Application.Queries;
using AppointmentService.Application.Abstractions;
using AppointmentService.Api.Security;
using AppointmentService.Domain.Entities;
using AppointmentService.Infrastructure.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AppointmentService.Api.Controllers;

[ApiController]
[Route("veterinarians")]
[Authorize]
public sealed class VeterinariansController(
    SearchVeterinariansHandler searchHandler,
    FindAvailableVeterinariansHandler findAvailableHandler,
    IVeterinarianRepository veterinarians,
    IUnitOfWork unitOfWork,
    KeycloakAdminClient keycloakAdmin) : ControllerBase
{
    /// <summary>GET /veterinarians?clinicId=&amp;specialization= — both filters are optional.</summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<VeterinarianDto>>> Search(
        [FromQuery] Guid? clinicId, [FromQuery] string? specialization, CancellationToken cancellationToken)
    {
        var result = await searchHandler.HandleAsync(new SearchVeterinariansQuery(clinicId, specialization), cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// GET /veterinarians/available?date=2026-08-18&amp;location=Skopje&amp;specialization=Surgery —
    /// veterinarians with at least one open slot on the given date, each with their open slots on
    /// that date attached. <c>date</c> is required; <c>location</c> and <c>specialization</c> are
    /// optional filters. This is the endpoint the shared MCP server's "find_open_appointment_slots"
    /// tool calls (see FindAvailableVeterinariansHandler for why it composes clinics + slots
    /// instead of a dedicated repository query).
    /// </summary>
    [HttpGet("available")]
    public async Task<ActionResult<IReadOnlyList<AvailableVeterinarianDto>>> Available(
        [FromQuery] DateOnly date, [FromQuery] string? location, [FromQuery] string? specialization,
        CancellationToken cancellationToken)
    {
        var result = await findAvailableHandler.HandleAsync(
            new FindAvailableVeterinariansQuery(date, location, specialization), cancellationToken);
        return Ok(result);
    }

    [HttpGet("{veterinarianId:guid}")]
    [Authorize(Roles = "veterinarian,admin")]
    public async Task<ActionResult<VeterinarianDto>> GetById(Guid veterinarianId, CancellationToken cancellationToken)
    {
        if (!UserOwnership.CanAccessVeterinarian(User, veterinarianId)) return Forbid();
        var veterinarian = await veterinarians.GetByIdAsync(veterinarianId, cancellationToken);
        return veterinarian is null ? NotFound() : Ok(veterinarian.ToDto());
    }

    [HttpPost]
    [Authorize(Roles = "admin")]
    public async Task<ActionResult<VeterinarianDto>> Create(
        CreateVeterinarianRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Username) ||
            string.IsNullOrWhiteSpace(request.TemporaryPassword) ||
            string.IsNullOrWhiteSpace(request.FullName) ||
            string.IsNullOrWhiteSpace(request.Specialization) ||
            string.IsNullOrWhiteSpace(request.LicenseNumber) ||
            request.ClinicId == Guid.Empty)
            return BadRequest(new ProblemDetails { Title = "Every veterinarian field is required." });

        var accountId = await keycloakAdmin.CreateVeterinarianAsync(
            request.Username, request.TemporaryPassword, request.FullName, cancellationToken);
        try
        {
            var veterinarian = Veterinarian.Seed(
                accountId, request.ClinicId, request.FullName, request.Specialization, request.LicenseNumber);
            await veterinarians.AddAsync(veterinarian, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return CreatedAtAction(nameof(GetById), new { veterinarianId = accountId }, veterinarian.ToDto());
        }
        catch
        {
            await keycloakAdmin.DeleteVeterinarianAsync(accountId, cancellationToken);
            throw;
        }
    }

    [HttpPut("{veterinarianId:guid}")]
    [Authorize(Roles = "veterinarian,admin")]
    public async Task<ActionResult<VeterinarianDto>> Update(Guid veterinarianId, UpdateVeterinarianProfileRequest request, CancellationToken cancellationToken)
    {
        if (!UserOwnership.CanAccessVeterinarian(User, veterinarianId)) return Forbid();
        var veterinarian = await veterinarians.GetByIdAsync(veterinarianId, cancellationToken);
        if (veterinarian is null) return NotFound();
        var administrator = User.IsInRole("admin");
        veterinarian.Update(
            request.FullName,
            request.Specialization,
            administrator ? request.LicenseNumber ?? veterinarian.LicenseNumber : veterinarian.LicenseNumber,
            administrator ? request.ClinicId : null);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        if (administrator)
            await keycloakAdmin.UpdateVeterinarianAsync(veterinarianId, request.FullName, cancellationToken);
        return Ok(veterinarian.ToDto());
    }

    [HttpDelete("{veterinarianId:guid}")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> Delete(Guid veterinarianId, CancellationToken cancellationToken)
    {
        var veterinarian = await veterinarians.GetByIdAsync(veterinarianId, cancellationToken);
        if (veterinarian is null) return NotFound();
        if (await veterinarians.HasAppointmentsAsync(veterinarianId, cancellationToken))
            return Conflict(new ProblemDetails
            {
                Title = "Veterinarian cannot be deleted",
                Detail = "This veterinarian has appointment history. Keep the profile so those records remain valid."
            });

        await keycloakAdmin.DeleteVeterinarianAsync(veterinarianId, cancellationToken);
        veterinarians.Remove(veterinarian);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return NoContent();
    }
}

public sealed record UpdateVeterinarianProfileRequest(
    string FullName,
    string Specialization,
    string? LicenseNumber = null,
    Guid? ClinicId = null);

public sealed record CreateVeterinarianRequest(
    string Username,
    string TemporaryPassword,
    string FullName,
    string Specialization,
    string LicenseNumber,
    Guid ClinicId);
