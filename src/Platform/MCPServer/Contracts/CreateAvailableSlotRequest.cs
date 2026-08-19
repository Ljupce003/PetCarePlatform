namespace MCPServer.Contracts;

public sealed record CreateAvailableSlotRequest(Guid VeterinarianId, DateTimeOffset StartsAtUtc, DateTimeOffset EndsAtUtc);
