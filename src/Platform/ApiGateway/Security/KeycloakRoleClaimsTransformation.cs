using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;

namespace ApiGateway.Security;

public sealed class KeycloakRoleClaimsTransformation(IConfiguration configuration) : IClaimsTransformation
{
    public Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        if (principal.Identity is not ClaimsIdentity { IsAuthenticated: true } identity)
            return Task.FromResult(principal);

        var clientId = configuration["Jwt:ClientId"] ?? "api-gateway";
        var roles = ReadRoles(identity.FindFirst("realm_access")?.Value)
            .Concat(ReadRoles(identity.FindFirst("resource_access")?.Value, clientId))
            .Distinct(StringComparer.Ordinal);

        foreach (var role in roles.Where(role => !identity.HasClaim(ClaimTypes.Role, role)))
            identity.AddClaim(new Claim(ClaimTypes.Role, role));

        return Task.FromResult(principal);
    }

    private static IReadOnlyList<string> ReadRoles(string? json, string? clientId = null)
    {
        if (string.IsNullOrWhiteSpace(json))
            return [];

        try
        {
            using var document = JsonDocument.Parse(json);
            var access = document.RootElement;
            if (clientId is not null &&
                (access.ValueKind != JsonValueKind.Object || !access.TryGetProperty(clientId, out access)))
                return [];

            if (access.ValueKind != JsonValueKind.Object ||
                !access.TryGetProperty("roles", out var roles) ||
                roles.ValueKind != JsonValueKind.Array)
                return [];

            return roles.EnumerateArray()
                .Where(role => role.ValueKind == JsonValueKind.String)
                .Select(role => role.GetString()!)
                .Where(role => !string.IsNullOrWhiteSpace(role))
                .ToArray();
        }
        catch (JsonException)
        {
            return [];
        }
    }
}
