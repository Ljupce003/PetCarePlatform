using AppointmentService.Infrastructure.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AppointmentService.Api.Controllers;

/// <summary>
/// POST /auth/login — logs in one of the demo users (owner1, vet1, admin1; see
/// infrastructure/keycloak/petcare-realm.json) and returns a JWT to paste into Swagger's Authorize
/// dialog. Outside Docker (local dev/tests, no real Keycloak expected to be running) this issues a
/// locally-signed token via <see cref="JwtTokenService"/> — the exact same
/// <c>useLegacyDevelopmentTokens</c> switch Program.cs uses to decide how it *validates* tokens.
/// In Docker it proxies the credentials to the real Keycloak realm via
/// <see cref="KeycloakAuthClient"/> and returns Keycloak's own token, so this is never issuing fake
/// tokens once Keycloak is actually in the picture.
/// </summary>
[ApiController]
[Route("auth")]
[AllowAnonymous]
public sealed class AuthController(
    JwtTokenService tokenService,
    KeycloakAuthClient keycloakAuthClient,
    IHostEnvironment environment,
    IConfiguration configuration) : ControllerBase
{
    [HttpPost("login")]
    public async Task<ActionResult<TokenResponse>> Login(LoginRequest request, CancellationToken cancellationToken)
    {
        var useLegacyDevelopmentTokens = !environment.IsEnvironment("Docker") &&
            !string.IsNullOrWhiteSpace(configuration["Jwt:SigningKey"]);

        if (useLegacyDevelopmentTokens)
        {
            var user = TestUsers.Find(request.Username, request.Password);
            if (user is null)
            {
                return Unauthorized(new { title = "Invalid username or password." });
            }

            var accessToken = tokenService.IssueUserToken(user.Id, user.Username, user.Role);
            return Ok(new TokenResponse(accessToken, user.Role, user.Id));
        }

        var result = await keycloakAuthClient.LoginAsync(request.Username, request.Password, cancellationToken);
        if (result is null)
        {
            return Unauthorized(new { title = "Invalid username or password." });
        }

        return Ok(new TokenResponse(result.AccessToken, result.Role, result.UserId));
    }
}

public sealed record LoginRequest(string Username, string Password);

public sealed record TokenResponse(string AccessToken, string? Role, Guid? UserId);
