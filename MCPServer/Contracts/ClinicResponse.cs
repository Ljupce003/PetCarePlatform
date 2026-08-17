namespace MCPServer.Contracts;

public sealed record ClinicResponse(Guid ClinicId, string Name, string Location, string Address);
