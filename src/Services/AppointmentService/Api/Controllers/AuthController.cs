using AppointmentService.Infrastructure.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AppointmentService.Api.Controllers;

/// <summary>
/// POST /auth/login — logs in one of the demo users (owner1, vet1, admin1; see
/// infrastructure/keycloak/petcare-realm.json). Everywhere this actually runs (local
/// <c>dotnet run</c>, Docker, production) it proxies to the real Keycloak realm via
/// <see cref="KeycloakAuthClient"/> (Resource Owner Password Credentials grant, public
/// <c>petcare-demo</c> client) and returns Keycloak's own token.
///
/// The one exception is the "Testing" environment (see AppointmentServiceApiFactory): CI has no
/// live Keycloak to reach, so <c>WebApplicationFactory</c>-driven integration tests get a
/// locally-signed token from <see cref="JwtTokenService"/>/<see cref="TestUsers"/> instead --
/// Program.cs's <c>AddJwtBearer</c> setup has the matching validation branch.
/// </summary>
[ApiController]
[Route("auth")]
[AllowAnonymous]
public sealed class AuthController(
    KeycloakAuthClient keycloakAuthClient,
    JwtTokenService tokenService,
    IHostEnvironment environment) : ControllerBase
{
    [HttpPost("login")]
    public async Task<ActionResult<TokenResponse>> Login(LoginRequest request, CancellationToken cancellationToken)
    {
        if (environment.IsEnvironment("Testing"))
        {
            var user = TestUsers.Find(request.Username, request.Password);
            if (user is null)
            {
                return Unauthorized(new { title = "Invalid username or password." });
            }

            var localToken = tokenService.IssueUserToken(user.Id, user.Username, user.Role);
            return Ok(new TokenResponse(localToken, user.Role, user.Id));
        }

        // KeycloakAuthClient only returns null for a Keycloak-issued rejection (bad credentials --
        // Keycloak responded, just not with 2xx). A connection failure (Keycloak unreachable,
        // DNS, timeout) throws instead, and deserves a distinct, obvious status instead of falling
        // through to Program.cs's generic 500 handler and leaving whoever's debugging this to
        // guess why the response body doesn't have the shape they expected.
        KeycloakLoginResult? result;
        try
        {
            result = await keycloakAuthClient.LoginAsync(request.Username, request.Password, cancellationToken);
        }
        catch (HttpRequestException exception)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new
            {
                title = "Could not reach Keycloak.",
                detail = exception.Message
            });
        }

        if (result is null)
        {
            return Unauthorized(new { title = "Invalid username or password." });
        }

        return Ok(new TokenResponse(result.AccessToken, result.Role, result.UserId));
    }
}

public sealed record LoginRequest(string Username, string Password);

public sealed record TokenResponse(string AccessToken, string? Role, Guid? UserId);
