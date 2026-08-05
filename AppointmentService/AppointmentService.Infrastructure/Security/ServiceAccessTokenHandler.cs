using System.Net.Http.Headers;

namespace AppointmentService.Infrastructure.Security;

/// <summary>
/// Supplies the (bearer) access token <see cref="ServiceAccessTokenHandler"/> attaches to
/// outgoing service-to-service requests.
/// </summary>
public interface IServiceAccessTokenProvider
{
    Task<string?> GetAccessTokenAsync(CancellationToken cancellationToken);
}

/// <summary>
/// No-op provider — kept around for reference/tests, but no longer registered by default now that
/// <see cref="LocalServiceAccessTokenProvider"/> issues a real (locally-signed) token instead.
/// Returns no token, so <see cref="ServiceAccessTokenHandler"/> sends requests unauthenticated.
/// </summary>
public sealed class NullServiceAccessTokenProvider : IServiceAccessTokenProvider
{
    public Task<string?> GetAccessTokenAsync(CancellationToken cancellationToken) => Task.FromResult<string?>(null);
}

/// <summary>
/// Requests a client-credentials-style access token for this service's own identity
/// (<see cref="TestClients.AppointmentService"/>) via <see cref="JwtTokenService"/> — a stand-in
/// for a real OAuth2 client-credentials request to Keycloak's token endpoint (see README).
/// Outgoing calls to the Pet Service now carry a real, signed bearer token; Pet Service just
/// doesn't validate it yet, since it doesn't have JWT bearer authentication wired up either.
/// </summary>
/// <remarks>
/// Swap this registration for a real Keycloak-backed implementation once Keycloak exists — one
/// that requests a token via the OAuth2 client-credentials grant over HTTP and caches it until
/// it's close to expiring. Nothing else needs to change: <see cref="ServiceAccessTokenHandler"/>
/// and every HttpClient it's attached to already only depend on <see cref="IServiceAccessTokenProvider"/>.
/// </remarks>
public sealed class LocalServiceAccessTokenProvider(JwtTokenService tokenService) : IServiceAccessTokenProvider
{
    public Task<string?> GetAccessTokenAsync(CancellationToken cancellationToken) =>
        Task.FromResult<string?>(tokenService.IssueServiceToken(TestClients.AppointmentService.ClientId));
}

/// <summary>
/// Attaches a client-credentials access token to every outgoing service-to-service request, via
/// whatever <see cref="IServiceAccessTokenProvider"/> is currently registered.
/// </summary>
public sealed class ServiceAccessTokenHandler(IServiceAccessTokenProvider tokenProvider) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var token = await tokenProvider.GetAccessTokenAsync(cancellationToken);
        if (!string.IsNullOrEmpty(token))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        return await base.SendAsync(request, cancellationToken);
    }
}
