using System.ComponentModel;
using System.Globalization;
using MCPServer.Clients;
using MCPServer.Contracts;
using ModelContextProtocol.Server;

namespace MCPServer.Tools;

[McpServerToolType]
public sealed class AppointmentTools(AppointmentServiceClient appointmentClient)
{
    [McpServerTool(Name = "find_available_veterinarians")]
    [Description("Finds veterinarians currently accepting appointments at all, optionally filtered by clinic and specialization. Does not check for open slots on any specific date -- use find_open_appointment_slots for that.")]
    public Task<IReadOnlyList<VeterinarianResponse>> FindAvailableVeterinarians(
        [Description("Optional clinic identifier.")] Guid? clinicId = null,
        [Description("Optional specialization such as surgery or dermatology.")] string? specialization = null,
        CancellationToken cancellationToken = default) =>
        appointmentClient.FindAvailableVeterinariansAsync(clinicId, specialization, cancellationToken);

    [McpServerTool(Name = "get_upcoming_appointments")]
    [Description("Gets the upcoming scheduled appointments for an owner.")]
    public Task<IReadOnlyList<AppointmentResponse>> GetUpcomingAppointments(
        [Description("The unique owner identifier.")] Guid ownerId,
        CancellationToken cancellationToken) =>
        appointmentClient.GetUpcomingAppointmentsAsync(ownerId, cancellationToken);

    [McpServerTool(Name = "search_clinics")]
    [Description("Lists veterinary clinics, optionally filtered by location/city.")]
    public Task<IReadOnlyList<ClinicResponse>> SearchClinics(
        [Description("Clinic city or location, for example Skopje. Omit to list every clinic.")] string? location = null,
        CancellationToken cancellationToken = default) =>
        appointmentClient.SearchClinicsAsync(location, cancellationToken);

    [McpServerTool(Name = "search_available_slots")]
    [Description("Lists open (not yet booked, not expired) appointment slots, optionally filtered by veterinarian and/or date.")]
    public Task<IReadOnlyList<AvailableSlotResponse>> SearchAvailableSlots(
        [Description("Optional veterinarian identifier to restrict results to a single veterinarian.")] Guid? veterinarianId = null,
        [Description("Optional date (yyyy-MM-dd) to restrict results to a single day.")] string? date = null,
        CancellationToken cancellationToken = default) =>
        appointmentClient.SearchAvailableSlotsAsync(veterinarianId, ParseOptionalDate(date), cancellationToken);

    [McpServerTool(Name = "find_open_appointment_slots")]
    [Description("Finds veterinarians with at least one open appointment slot on a given date, optionally narrowed by clinic location and/or specialization. Returns each matching veterinarian together with their open slots on that date -- this is the tool for \"who is free on <date>\" questions.")]
    public Task<IReadOnlyList<OpenAppointmentSlotsResponse>> FindOpenAppointmentSlots(
        [Description("Date to search, formatted as yyyy-MM-dd.")] string date,
        [Description("Clinic city or location, for example Skopje.")] string? location = null,
        [Description("Veterinarian specialization, for example 'General Practice'.")] string? specialization = null,
        CancellationToken cancellationToken = default) =>
        appointmentClient.FindOpenAppointmentSlotsAsync(ParseRequiredDate(date), location, specialization, cancellationToken);

    [McpServerTool(Name = "create_available_slot")]
    [Description("Opens a new appointment slot for the veterinarian identified by veterinarianId. The trusted MCP service account performs this administrative action.")]
    public Task<AvailableSlotResponse> CreateAvailableSlot(
        [Description("The veterinarian this slot belongs to.")] Guid veterinarianId,
        [Description("Slot start, ISO 8601 with offset, for example 2026-08-18T09:00:00Z.")] DateTimeOffset startsAtUtc,
        [Description("Slot end, ISO 8601 with offset, for example 2026-08-18T09:30:00Z.")] DateTimeOffset endsAtUtc,
        CancellationToken cancellationToken) =>
        appointmentClient.CreateAvailableSlotAsync(
            new CreateAvailableSlotRequest(veterinarianId, startsAtUtc, endsAtUtc), cancellationToken);

    // Tool parameters take the date as a plain string (yyyy-MM-dd) and are parsed explicitly here,
    // for a clear error on a bad value instead of relying on the SDK's own DateOnly binding.
    //
    // The actual bug that broke search_available_slots/find_open_appointment_slots, though, was
    // every optional parameter above missing a C# default value: AIFunctionFactory's JSON schema
    // generation marks a parameter "required" whenever it has no default, regardless of its type
    // being nullable -- so calling the tool without that argument (as any caller reasonably would
    // for an optional filter) failed schema validation before this method ever ran. `= null` /
    // `= default` on every optional parameter is the fix; the nullable type alone isn't enough.
    private static DateOnly? ParseOptionalDate(string? date) =>
        string.IsNullOrWhiteSpace(date) ? null : ParseRequiredDate(date);

    private static DateOnly ParseRequiredDate(string date) =>
        DateOnly.TryParse(date, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed)
            ? parsed
            : throw new ArgumentException($"'{date}' is not a valid date. Expected format: yyyy-MM-dd.", nameof(date));
}
