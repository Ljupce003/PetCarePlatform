using System.ComponentModel;
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
        [Description("Optional clinic identifier.")] Guid? clinicId,
        [Description("Optional specialization such as surgery or dermatology.")] string? specialization,
        CancellationToken cancellationToken) =>
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
        [Description("Clinic city or location, for example Skopje. Omit to list every clinic.")] string? location,
        CancellationToken cancellationToken) =>
        appointmentClient.SearchClinicsAsync(location, cancellationToken);

    [McpServerTool(Name = "search_available_slots")]
    [Description("Lists open (not yet booked, not expired) appointment slots, optionally filtered by veterinarian and/or date.")]
    public Task<IReadOnlyList<AvailableSlotResponse>> SearchAvailableSlots(
        [Description("Optional veterinarian identifier to restrict results to a single veterinarian.")] Guid? veterinarianId,
        [Description("Optional date (yyyy-MM-dd) to restrict results to a single day.")] DateOnly? date,
        CancellationToken cancellationToken) =>
        appointmentClient.SearchAvailableSlotsAsync(veterinarianId, date, cancellationToken);

    [McpServerTool(Name = "find_open_appointment_slots")]
    [Description("Finds veterinarians with at least one open appointment slot on a given date, optionally narrowed by clinic location and/or specialization. Returns each matching veterinarian together with their open slots on that date -- this is the tool for \"who is free on <date>\" questions.")]
    public Task<IReadOnlyList<OpenAppointmentSlotsResponse>> FindOpenAppointmentSlots(
        [Description("Date to search, formatted as yyyy-MM-dd.")] DateOnly date,
        [Description("Clinic city or location, for example Skopje.")] string? location,
        [Description("Veterinarian specialization, for example 'General Practice'.")] string? specialization,
        CancellationToken cancellationToken) =>
        appointmentClient.FindOpenAppointmentSlotsAsync(date, location, specialization, cancellationToken);

    [McpServerTool(Name = "create_available_slot")]
    [Description("Opens a new appointment slot for an existing veterinarian. Administrative action -- requires an administrator token, forwarded as-is to the Appointment Service.")]
    public Task<AvailableSlotResponse> CreateAvailableSlot(
        [Description("The veterinarian this slot belongs to.")] Guid veterinarianId,
        [Description("Slot start, ISO 8601 with offset, for example 2026-08-18T09:00:00Z.")] DateTimeOffset startsAtUtc,
        [Description("Slot end, ISO 8601 with offset, for example 2026-08-18T09:30:00Z.")] DateTimeOffset endsAtUtc,
        CancellationToken cancellationToken) =>
        appointmentClient.CreateAvailableSlotAsync(
            new CreateAvailableSlotRequest(veterinarianId, startsAtUtc, endsAtUtc), cancellationToken);
}
