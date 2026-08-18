using MCPServer.Contracts;

namespace MCPServer.Clients;

public sealed class AppointmentServiceClient(HttpClient httpClient)
{
    public async Task<IReadOnlyList<VeterinarianResponse>> FindAvailableVeterinariansAsync(
        Guid? clinicId,
        string? specialization,
        CancellationToken cancellationToken)
    {
        var query = new List<string>();
        if (clinicId.HasValue)
            query.Add($"clinicId={clinicId.Value:D}");
        if (!string.IsNullOrWhiteSpace(specialization))
            query.Add($"specialization={Uri.EscapeDataString(specialization.Trim())}");

        var path = query.Count == 0 ? "veterinarians" : $"veterinarians?{string.Join('&', query)}";
        var veterinarians = await httpClient.GetFromJsonAsync<List<VeterinarianResponse>>(path, cancellationToken)
            ?? [];

        return veterinarians.Where(veterinarian => veterinarian.IsAvailable).ToList();
    }

    public async Task<IReadOnlyList<AppointmentResponse>> GetUpcomingAppointmentsAsync(
        Guid ownerId,
        CancellationToken cancellationToken)
    {
        var appointments = await httpClient.GetFromJsonAsync<List<AppointmentResponse>>(
            $"appointments/upcoming?ownerId={ownerId:D}",
            cancellationToken);
        return appointments ?? [];
    }

    public async Task<IReadOnlyList<ClinicResponse>> SearchClinicsAsync(
        string? location,
        CancellationToken cancellationToken)
    {
        var path = string.IsNullOrWhiteSpace(location)
            ? "clinics"
            : $"clinics?location={Uri.EscapeDataString(location.Trim())}";
        var clinics = await httpClient.GetFromJsonAsync<List<ClinicResponse>>(path, cancellationToken);
        return clinics ?? [];
    }

    public async Task<IReadOnlyList<AvailableSlotResponse>> SearchAvailableSlotsAsync(
        Guid? veterinarianId,
        DateOnly? date,
        CancellationToken cancellationToken)
    {
        var query = new List<string>();
        if (veterinarianId.HasValue)
            query.Add($"veterinarianId={veterinarianId.Value:D}");
        if (date.HasValue)
            query.Add($"date={date.Value:yyyy-MM-dd}");

        var path = query.Count == 0 ? "slots" : $"slots?{string.Join('&', query)}";
        var slots = await httpClient.GetFromJsonAsync<List<AvailableSlotResponse>>(path, cancellationToken);
        return slots ?? [];
    }

    /// <summary>
    /// Calls <c>GET /veterinarians/available</c> -- veterinarians with at least one open slot on
    /// the given date, unlike <see cref="FindAvailableVeterinariansAsync"/> which only reflects
    /// the veterinarian's general <c>IsAvailable</c> flag.
    /// </summary>
    public async Task<IReadOnlyList<OpenAppointmentSlotsResponse>> FindOpenAppointmentSlotsAsync(
        DateOnly date,
        string? location,
        string? specialization,
        CancellationToken cancellationToken)
    {
        var query = new List<string> { $"date={date:yyyy-MM-dd}" };
        if (!string.IsNullOrWhiteSpace(location))
            query.Add($"location={Uri.EscapeDataString(location.Trim())}");
        if (!string.IsNullOrWhiteSpace(specialization))
            query.Add($"specialization={Uri.EscapeDataString(specialization.Trim())}");

        var results = await httpClient.GetFromJsonAsync<List<OpenAppointmentSlotsResponse>>(
            $"veterinarians/available?{string.Join('&', query)}", cancellationToken);
        return results ?? [];
    }

    /// <summary>
    /// Calls <c>POST /slots</c> (admin-only downstream). The caller's own bearer token is forwarded
    /// by <see cref="BearerTokenForwardingHandler"/>, so Appointment Service enforces the admin role
    /// itself -- this client never impersonates anyone.
    /// </summary>
    public async Task<AvailableSlotResponse> CreateAvailableSlotAsync(
        CreateAvailableSlotRequest request,
        CancellationToken cancellationToken)
    {
        using var response = await httpClient.PostAsJsonAsync("slots", request, cancellationToken);
        await DownstreamResponse.EnsureSuccessAsync("Appointment Service", response, cancellationToken);
        return await response.Content.ReadFromJsonAsync<AvailableSlotResponse>(cancellationToken: cancellationToken)
            ?? throw new InvalidOperationException("Appointment Service returned an empty response.");
    }
}
