using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace AppointmentService.Infrastructure.Security;

/// <summary>
/// Issues locally-signed, HMAC-SHA256 user JWTs only for <c>AuthController.Login</c>'s
/// "Testing"-environment branch (CI has no live Keycloak -- see
/// AppointmentService.Api.IntegrationTests). Docker and normal runtime authentication use the
/// real Keycloak realm; service-to-service tokens use Keycloak's client-credentials grant.
/// </summary>
public sealed class JwtTokenService(IOptions<JwtOptions> options)
{
    /// <summary>Issues a token for a human user logged in via <c>POST /auth/login</c> in the "Testing" environment only.</summary>
    public string IssueUserToken(Guid userId, string username, string role) =>
        CreateToken([
            new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new Claim(JwtRegisteredClaimNames.UniqueName, username),
            new Claim(ClaimTypes.Role, role)
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
