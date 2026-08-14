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
}
