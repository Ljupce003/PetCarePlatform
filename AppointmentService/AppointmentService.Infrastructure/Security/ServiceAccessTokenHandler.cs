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
/// No-op provider used until the platform has an identity provider to request a token from.
/// Returns no token, so <see cref="ServiceAccessTokenHandler"/> sends requests unauthenticated.
/// </summary>
/// <remarks>
/// Swap this registration for a real implementation once Keycloak (or another IdP) exists —
/// one that requests a token via the OAuth2 client-credentials grant and caches it until it's
/// close to expiring. Nothing else needs to change: <see cref="ServiceAccessTokenHandler"/> and
/// every HttpClient it's attached to already only depend on <see cref="IServiceAccessTokenProvider"/>.
/// </remarks>
public sealed class NullServiceAccessTokenProvider : IServiceAccessTokenProvider
{
    public Task<string?> GetAccessTokenAsync(CancellationToken cancellationToken) => Task.FromResult<string?>(null);
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
