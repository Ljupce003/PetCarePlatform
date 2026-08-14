using System.ComponentModel;
using MCPServer.Clients;
using MCPServer.Contracts;
using ModelContextProtocol.Server;

namespace MCPServer.Tools;

[McpServerToolType]
public sealed class AppointmentTools(AppointmentServiceClient appointmentClient)
{
    [McpServerTool(Name = "find_available_veterinarians")]
    [Description("Finds currently available veterinarians, optionally filtered by clinic and specialization.")]
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
}
