using AppointmentService.Infrastructure.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AppointmentService.Api.Controllers;

/// <summary>
/// POST /auth/login — logs in one of the demo users (owner1, vet1, admin1; see
/// infrastructure/keycloak/petcare-realm.json) against the real Keycloak realm via
/// <see cref="KeycloakAuthClient"/> (Resource Owner Password Credentials grant, public
/// <c>petcare-demo</c> client) and returns Keycloak's own token — no locally-issued fallback.
/// Requires a reachable Keycloak wherever this runs, including tests (see
/// AppointmentServiceApiFactory).
/// </summary>
[ApiController]
[Route("auth")]
[AllowAnonymous]
public sealed class AuthController(KeycloakAuthClient keycloakAuthClient) : ControllerBase
{
    [HttpPost("login")]
    public async Task<ActionResult<TokenResponse>> Login(LoginRequest request, CancellationToken cancellationToken)
    {
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
