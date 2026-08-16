using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;

namespace AppointmentService.Infrastructure.Security;

/// <summary>
/// Supplies the (bearer) access token <see cref="ServiceAccessTokenHandler"/> attaches to
/// outgoing service-to-service requests.
/// </summary>
public interface IServiceAccessTokenProvider
{
    Task<string?> GetAccessTokenAsync(CancellationToken cancellationToken);
}

public sealed class KeycloakServiceAccessTokenOptions
{
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public int RefreshSkewSeconds { get; set; } = 30;
}

/// <summary>
/// Obtains Appointment Service's own OAuth 2.0 access token from Keycloak using the
/// client-credentials grant. The token is cached until shortly before its advertised expiry so
/// concurrent Pet Service calls do not create a token request per appointment.
/// </summary>
public sealed class KeycloakServiceAccessTokenProvider(
    IHttpClientFactory httpClientFactory,
    IOptions<KeycloakServiceAccessTokenOptions> options,
    TimeProvider timeProvider) : IServiceAccessTokenProvider, IDisposable
{
    private readonly SemaphoreSlim _refreshLock = new(1, 1);
    private string? _accessToken;
    private DateTimeOffset _expiresAtUtc;

    public async Task<string?> GetAccessTokenAsync(CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        if (IsFresh(now))
        {
            return _accessToken;
        }

        await _refreshLock.WaitAsync(cancellationToken);
        try
        {
            now = timeProvider.GetUtcNow();
            if (IsFresh(now))
            {
                return _accessToken;
            }

            var settings = options.Value;
            var client = httpClientFactory.CreateClient("keycloak-service-token");
            using var response = await client.PostAsync(
                "protocol/openid-connect/token",
                new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["grant_type"] = "client_credentials",
                    ["client_id"] = settings.ClientId,
                    ["client_secret"] = settings.ClientSecret
                }),
                cancellationToken);

            response.EnsureSuccessStatusCode();
            var payload = await response.Content.ReadFromJsonAsync<KeycloakTokenResponse>(cancellationToken: cancellationToken)
                ?? throw new InvalidOperationException("Keycloak returned an empty service-token response.");
            if (string.IsNullOrWhiteSpace(payload.AccessToken) || payload.ExpiresIn <= 0)
            {
                throw new InvalidOperationException("Keycloak returned an invalid service-token response.");
            }

            _accessToken = payload.AccessToken;
            _expiresAtUtc = now.AddSeconds(payload.ExpiresIn);
            return _accessToken;
        }
        finally
        {
            _refreshLock.Release();
        }
    }

    private bool IsFresh(DateTimeOffset now)
    {
        var skew = TimeSpan.FromSeconds(Math.Max(0, options.Value.RefreshSkewSeconds));
        return !string.IsNullOrWhiteSpace(_accessToken) && now < _expiresAtUtc.Subtract(skew);
    }

    public void Dispose() => _refreshLock.Dispose();

    private sealed record KeycloakTokenResponse(
        [property: JsonPropertyName("access_token")] string AccessToken,
        [property: JsonPropertyName("expires_in")] int ExpiresIn);
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
