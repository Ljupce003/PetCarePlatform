using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AppointmentService.Infrastructure.Security;

/// <summary>
/// The only login path <see cref="AuthController"/> has — no locally-issued fallback. Proxies a
/// username/password to Keycloak's own token endpoint via the Resource Owner Password Credentials
/// grant, using the public <c>petcare-demo</c> client (public, no secret,
/// <c>directAccessGrantsEnabled: true</c> — see infrastructure/keycloak/petcare-realm.json), so a
/// Swagger user always gets a genuine Keycloak-signed token.
/// </summary>
public sealed class KeycloakAuthClient(HttpClient httpClient)
{
    private const string DemoClientId = "petcare-demo";

    public async Task<KeycloakLoginResult?> LoginAsync(string username, string password, CancellationToken cancellationToken)
    {
        using var response = await httpClient.PostAsync(
            "protocol/openid-connect/token",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "password",
                ["client_id"] = DemoClientId,
                ["username"] = username,
                ["password"] = password
            }),
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        var token = await response.Content.ReadFromJsonAsync<KeycloakTokenResponse>(cancellationToken: cancellationToken);
        if (token is null || string.IsNullOrWhiteSpace(token.AccessToken))
        {
            return null;
        }

        return new KeycloakLoginResult(token.AccessToken, token.ExpiresIn, ReadRole(token.AccessToken), ReadUserId(token.AccessToken));
    }

    // Keycloak puts realm roles in a "realm_access": { "roles": [...] } claim, not the single
    // ClaimTypes.Role claim KeycloakRoleClaimsTransformation produces once ASP.NET has actually
    // authenticated a request — this just peeks at the raw token to surface a role in the response.
    private static string? ReadRole(string accessToken)
    {
        var realmAccessJson = new JwtSecurityTokenHandler().ReadJwtToken(accessToken)
            .Claims.FirstOrDefault(claim => claim.Type == "realm_access")?.Value;
        if (realmAccessJson is null)
        {
            return null;
        }

        using var document = JsonDocument.Parse(realmAccessJson);
        if (!document.RootElement.TryGetProperty("roles", out var roles))
        {
            return null;
        }

        string[] knownRoles = ["owner", "veterinarian", "admin", "service"];
        return roles.EnumerateArray()
            .Select(role => role.GetString())
            .FirstOrDefault(role => knownRoles.Contains(role));
    }

    private static Guid? ReadUserId(string accessToken)
    {
        var subject = new JwtSecurityTokenHandler().ReadJwtToken(accessToken).Subject;
        return Guid.TryParse(subject, out var userId) ? userId : null;
    }

    private sealed record KeycloakTokenResponse(
        [property: JsonPropertyName("access_token")] string AccessToken,
        [property: JsonPropertyName("expires_in")] int ExpiresIn);
}

public sealed record KeycloakLoginResult(string AccessToken, int ExpiresInSeconds, string? Role, Guid? UserId);
