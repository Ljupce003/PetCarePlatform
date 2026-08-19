namespace AppointmentService.Infrastructure.Security;

/// <summary>
/// JWT settings used by the isolated integration-test token issuer.
/// </summary>
public sealed class JwtOptions
{
    public string Issuer { get; set; } = "appointment-service";
    public string Audience { get; set; } = "petcare";
    public string SigningKey { get; set; } = string.Empty;
    public int AccessTokenLifetimeMinutes { get; set; } = 60;
}
