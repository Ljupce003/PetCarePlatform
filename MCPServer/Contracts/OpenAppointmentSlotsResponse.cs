namespace MCPServer.Contracts;

public sealed record AvailableSlotSummaryResponse(Guid AvailabilitySlotId, DateTimeOffset StartsAtUtc, DateTimeOffset EndsAtUtc);

/// <summary>
/// One veterinarian with at least one open slot on the requested date, together with those slots.
/// Returned by Appointment Service's <c>GET /veterinarians/available</c>, which is what
/// <c>find_open_appointment_slots</c> calls -- distinct from <c>find_available_veterinarians</c>,
/// which only reflects a veterinarian's general <c>IsAvailable</c> flag and knows nothing about
/// slots or dates.
/// </summary>
public sealed record OpenAppointmentSlotsResponse(
    Guid VeterinarianId,
    string VeterinarianName,
    string Specialization,
    Guid ClinicId,
    string ClinicName,
    IReadOnlyList<AvailableSlotSummaryResponse> AvailableSlots);
