using System.ComponentModel;
using AppointmentService.Application.Commands;
using AppointmentService.Application.Dtos;
using AppointmentService.Application.Queries;
using ModelContextProtocol.Server;

namespace AppointmentService.Api.Mcp;

/// <summary>
/// Member 2's contribution to the shared MCP server (Member-2-Tasks.md, section 10) -- exposed
/// directly from this service's own process at <c>/mcp</c> (see Program.cs), not a separate
/// project. Each tool is a thin wrapper around an existing Application-layer handler (the same
/// ones ClinicsController/VeterinariansController/AvailabilitySlotsController/AppointmentsController
/// call), so there's no second copy of any business rule and no extra network hop -- the
/// ModelContextProtocol SDK resolves this class (and its handler constructor parameters) from the
/// very same DI container as the rest of the API.
///
/// Mostly read-only: booking, cancelling and rescheduling stay REST-only endpoints
/// (AppointmentsController), since those actions need a specific, authenticated owner/admin -- an
/// MCP tool call here has no such per-user identity to act as. <see cref="CreateAvailableSlot"/> is
/// the one deliberate exception: opening a new slot is an administrative/scheduling action, not
/// something done on behalf of a specific owner, so it doesn't have the same "who is this for"
/// problem the booking actions do.
/// </summary>
[McpServerToolType]
public sealed class AppointmentTools(
    SearchClinicsHandler searchClinics,
    SearchVeterinariansHandler searchVeterinarians,
    SearchAvailableSlotsHandler searchAvailableSlots,
    FindAvailableVeterinariansHandler findAvailableVeterinarians,
    GetUpcomingAppointmentsHandler getUpcomingAppointments,
    CreateAvailabilitySlotHandler createAvailabilitySlot)
{
    [McpServerTool, Description("Lists veterinary clinics, optionally filtered by location/city.")]
    public Task<IReadOnlyList<ClinicDto>> SearchClinics(
        [Description("Clinic city or location, for example Skopje. Omit to list every clinic.")] string? location = null,
        CancellationToken cancellationToken = default) =>
        searchClinics.HandleAsync(new SearchClinicsQuery(location), cancellationToken);

    [McpServerTool, Description("Lists veterinarians, optionally filtered by clinic and/or specialization.")]
    public Task<IReadOnlyList<VeterinarianDto>> SearchVeterinarians(
        [Description("Clinic GUID to restrict results to a single clinic")] string? clinicId = null,
        [Description("Veterinarian specialization, for example 'General Practice'")] string? specialization = null,
        CancellationToken cancellationToken = default) =>
        searchVeterinarians.HandleAsync(
            new SearchVeterinariansQuery(string.IsNullOrWhiteSpace(clinicId) ? null : Guid.Parse(clinicId), specialization),
            cancellationToken);

    [McpServerTool, Description("Lists open (not yet booked, not expired) availability slots, optionally filtered by veterinarian and/or date.")]
    public Task<IReadOnlyList<AvailableSlotDto>> SearchAvailableSlots(
        [Description("Veterinarian GUID to restrict results to a single veterinarian")] string? veterinarianId = null,
        [Description("Date formatted as yyyy-MM-dd")] string? date = null,
        CancellationToken cancellationToken = default) =>
        searchAvailableSlots.HandleAsync(
            new SearchAvailableSlotsQuery(
                string.IsNullOrWhiteSpace(veterinarianId) ? null : Guid.Parse(veterinarianId),
                string.IsNullOrWhiteSpace(date) ? null : DateOnly.Parse(date)),
            cancellationToken);

    [McpServerTool, Description(
        "Finds veterinarians with at least one open appointment slot on a given date, optionally " +
        "narrowed down by clinic location and/or specialization. Returns each matching veterinarian " +
        "together with their open slots on that date -- this is the tool to use for \"who is free " +
        "on <date>\" style questions, instead of combining SearchClinics/SearchVeterinarians/SearchAvailableSlots by hand.")]
    public Task<IReadOnlyList<AvailableVeterinarianDto>> FindAvailableVeterinarians(
        [Description("Date formatted as yyyy-MM-dd")] string date,
        [Description("Clinic city or location, for example Skopje")] string? location = null,
        [Description("Veterinarian specialization, for example 'General Practice'")] string? specialization = null,
        CancellationToken cancellationToken = default) =>
        findAvailableVeterinarians.HandleAsync(
            new FindAvailableVeterinariansQuery(DateOnly.Parse(date), location, specialization),
            cancellationToken);

    [McpServerTool, Description("Returns the upcoming (still-scheduled) appointments for a pet owner.")]
    public Task<IReadOnlyList<AppointmentDto>> GetUpcomingAppointments(
        [Description("Owner GUID")] string ownerId, CancellationToken cancellationToken = default) =>
        getUpcomingAppointments.HandleAsync(new GetUpcomingAppointmentsQuery(Guid.Parse(ownerId)), cancellationToken);

    [McpServerTool, Description(
        "Opens a new open availability slot for an existing veterinarian. Administrative action " +
        "(equivalent to POST /slots with an admin token) -- use it to add slots on dates the demo " +
        "seed data doesn't already cover.")]
    public Task<AvailableSlotDto> CreateAvailableSlot(
        [Description("Veterinarian GUID")] string veterinarianId,
        [Description("Slot start, ISO 8601 with offset, for example 2026-08-18T09:00:00Z")] string startsAtUtc,
        [Description("Slot end, ISO 8601 with offset, for example 2026-08-18T09:30:00Z")] string endsAtUtc,
        CancellationToken cancellationToken = default) =>
        createAvailabilitySlot.HandleAsync(
            new CreateAvailabilitySlotCommand(Guid.Parse(veterinarianId), DateTimeOffset.Parse(startsAtUtc), DateTimeOffset.Parse(endsAtUtc)),
            cancellationToken);
}
