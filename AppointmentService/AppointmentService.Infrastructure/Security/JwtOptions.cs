namespace AppointmentService.Infrastructure.Security;

/// <summary>
/// Settings for the locally-signed JWTs this service issues from <c>/auth/login</c> and
/// <c>/auth/token</c>, and validates on every incoming request. Stands in for Keycloak (see
/// README) until that exists — <see cref="SigningKey"/> must be at least 32 characters (HMAC-SHA256).
/// </summary>
public sealed class JwtOptions
{
    public string Issuer { get; set; } = "appointment-service";
    public string Audience { get; set; } = "petcare";
    public string SigningKey { get; set; } = string.Empty;
    public int AccessTokenLifetimeMinutes { get; set; } = 60;
}
