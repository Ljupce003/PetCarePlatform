namespace AppointmentService.Infrastructure.Security;

/// <summary>
/// Settings for the locally-signed JWTs this service falls back to outside Docker: from
/// <c>/auth/login</c> (when no real Keycloak is expected to be reachable) and for the
/// service-to-service token <see cref="LocalServiceAccessTokenProvider"/> issues. Also supplies
/// <c>Authority</c>/<c>Issuer</c>/<c>Audience</c> for real Keycloak-backed validation in Docker
/// (see README) — <see cref="SigningKey"/> itself must be at least 32 characters (HMAC-SHA256).
/// </summary>
public sealed class JwtOptions
{
    public string Issuer { get; set; } = "appointment-service";
    public string Audience { get; set; } = "petcare";
    public string SigningKey { get; set; } = string.Empty;
    public int AccessTokenLifetimeMinutes { get; set; } = 60;
}
