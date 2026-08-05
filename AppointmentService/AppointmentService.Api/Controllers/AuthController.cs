using AppointmentService.Infrastructure.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AppointmentService.Api.Controllers;

/// <summary>
/// Local stand-in for Keycloak (see README's "Security and authorization" section) — issues the
/// same shape of JWT a real identity provider would, just without a real user/client store behind it.
/// </summary>
[ApiController]
[Route("auth")]
[AllowAnonymous]
public sealed class AuthController(JwtTokenService tokenService) : ControllerBase
{
    /// <summary>
    /// POST /auth/login — logs in as one of the fixed demo users (see TestUsers: owner1, vet1,
    /// admin1) and returns a JWT. Paste the token into Swagger's Authorize dialog to call
    /// protected endpoints as that role.
    /// </summary>
    [HttpPost("login")]
    public ActionResult<TokenResponse> Login(LoginRequest request)
    {
        var user = TestUsers.Find(request.Username, request.Password);
        if (user is null)
        {
            return Unauthorized(new { title = "Invalid username or password." });
        }

        var accessToken = tokenService.IssueUserToken(user.Id, user.Username, user.Role);
        return Ok(new TokenResponse(accessToken, user.Role, user.Id));
    }

    /// <summary>
    /// POST /auth/token — OAuth2 client-credentials-style token for service-to-service calls
    /// (see TestClients: appointment-service / appointment-secret). Not meant to be called from
    /// Swagger by a person — this is what LocalServiceAccessTokenProvider effectively stands in
    /// for when calling the Pet Service.
    /// </summary>
    [HttpPost("token")]
    public ActionResult<TokenResponse> Token(ClientCredentialsRequest request)
    {
        var client = TestClients.Find(request.ClientId, request.ClientSecret);
        if (client is null)
        {
            return Unauthorized(new { title = "Invalid client credentials." });
        }

        var accessToken = tokenService.IssueServiceToken(client.ClientId);
        return Ok(new TokenResponse(accessToken, "service", null));
    }
}

public sealed record LoginRequest(string Username, string Password);

public sealed record ClientCredentialsRequest(string ClientId, string ClientSecret);

public sealed record TokenResponse(string AccessToken, string Role, Guid? UserId);
