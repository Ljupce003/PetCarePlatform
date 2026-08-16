namespace AppointmentService.Infrastructure.Security;

/// <summary>
/// Settings for real Keycloak-backed token validation (<c>Authority</c>/<c>Issuer</c>/
/// <c>Audience</c>, see README's "Security and authorization"), plus the locally-signed test-user
/// JWT used only by the integration-test authentication path. <see cref="SigningKey"/> must be at
/// least 32 characters for that HMAC-SHA256 test token.
/// </summary>
public sealed class JwtOptions
{
    public string Issuer { get; set; } = "appointment-service";
    public string Audience { get; set; } = "petcare";
    public string SigningKey { get; set; } = string.Empty;
    public int AccessTokenLifetimeMinutes { get; set; } = 60;
}
