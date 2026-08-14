namespace AppointmentService.Infrastructure.Security;

/// <summary>
/// Settings for real Keycloak-backed token validation (<c>Authority</c>/<c>Issuer</c>/
/// <c>Audience</c>, see README's "Security and authorization"), plus the one remaining
/// locally-signed JWT this service still issues: the service-to-service token
/// <see cref="LocalServiceAccessTokenProvider"/> hands out for calls to Pet Service
/// (<see cref="SigningKey"/> must be at least 32 characters, HMAC-SHA256).
/// </summary>
public sealed class JwtOptions
{
    public string Issuer { get; set; } = "appointment-service";
    public string Audience { get; set; } = "petcare";
    public string SigningKey { get; set; } = string.Empty;
    public int AccessTokenLifetimeMinutes { get; set; } = 60;
}
