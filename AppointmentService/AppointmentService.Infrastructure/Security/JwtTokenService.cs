using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace AppointmentService.Infrastructure.Security;

/// <summary>
/// Issues a locally-signed, HMAC-SHA256 JWT for <see cref="LocalServiceAccessTokenProvider"/>'s
/// service-to-service calls to Pet Service. This is the one remaining local token issuer in the
/// service — human login (<c>POST /auth/login</c>) goes through <see cref="KeycloakAuthClient"/>
/// against the real Keycloak realm instead; see README's "Service-to-service authentication" for
/// what replaces this once that call is switched to a real Keycloak client-credentials grant too.
/// </summary>
public sealed class JwtTokenService(IOptions<JwtOptions> options)
{
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
