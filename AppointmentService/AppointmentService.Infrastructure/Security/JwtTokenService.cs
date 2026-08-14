using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace AppointmentService.Infrastructure.Security;

/// <summary>
/// Issues and (via <see cref="Options"/>, consumed by Program.cs's AddJwtBearer setup) validates
/// this service's own JWTs. Everything about this class is a stand-in for a real identity
/// provider — see the "Security and authorization" section of README for exactly what changes
/// once Keycloak exists.
/// </summary>
public sealed class JwtTokenService(IOptions<JwtOptions> options)
{
    /// <summary>Issues a token for a human user logged in via <c>POST /auth/login</c>.</summary>
    public string IssueUserToken(Guid userId, string username, string role) =>
        CreateToken([
            new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new Claim(JwtRegisteredClaimNames.UniqueName, username),
            new Claim(ClaimTypes.Role, role)
        ]);

    /// <summary>Issues a locally-signed service-to-service token for <see cref="LocalServiceAccessTokenProvider"/>.</summary>
    public string IssueServiceToken(string clientId) =>
        CreateToken([
            new Claim(JwtRegisteredClaimNames.Sub, clientId),
            new Claim("client_id", clientId),
            new Claim(ClaimTypes.Role, "service")
        ]);

    private string CreateToken(IEnumerable<Claim> claims)
    {
        var jwt = options.Value;
        if (string.IsNullOrWhiteSpace(jwt.SigningKey))
        {
            throw new InvalidOperationException(
                "Jwt:SigningKey is not configured. Set Jwt:SigningKey in appsettings or the Jwt__SigningKey environment variable.");
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SigningKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: jwt.Issuer,
            audience: jwt.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(jwt.AccessTokenLifetimeMinutes),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
