using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;

namespace AppointmentService.Infrastructure.Security;

public interface IServiceAccessTokenProvider
{
    Task<string> GetAccessTokenAsync(CancellationToken cancellationToken);
}

public sealed class KeycloakServiceAccessTokenProvider(
    IHttpClientFactory httpClientFactory,
    IConfiguration configuration) : IServiceAccessTokenProvider
{
    private readonly SemaphoreSlim _refreshLock = new(1, 1);
    private string? _accessToken;
    private DateTimeOffset _refreshAt;

    public async Task<string> GetAccessTokenAsync(CancellationToken cancellationToken)
    {
        if (_accessToken is not null && DateTimeOffset.UtcNow < _refreshAt)
            return _accessToken;

        await _refreshLock.WaitAsync(cancellationToken);
        try
        {
            if (_accessToken is not null && DateTimeOffset.UtcNow < _refreshAt)
                return _accessToken;

            var clientId = configuration["ServiceAuthentication:ClientId"]
                ?? throw new InvalidOperationException("ServiceAuthentication:ClientId is required.");
            var clientSecret = configuration["ServiceAuthentication:ClientSecret"]
                ?? throw new InvalidOperationException("ServiceAuthentication:ClientSecret is required.");

            using var content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "client_credentials",
                ["client_id"] = clientId,
                ["client_secret"] = clientSecret
            });
            using var response = await httpClientFactory.CreateClient("keycloak-service-token")
                .PostAsync("protocol/openid-connect/token", content, cancellationToken);
            response.EnsureSuccessStatusCode();

            var token = await response.Content.ReadFromJsonAsync<KeycloakTokenResponse>(cancellationToken)
                ?? throw new InvalidOperationException("Keycloak returned an empty token response.");
            _accessToken = token.AccessToken;
            _refreshAt = DateTimeOffset.UtcNow.AddSeconds(Math.Max(1, token.ExpiresIn - 30));
            return _accessToken;
        }
        finally
        {
            _refreshLock.Release();
        }
    }

    private sealed record KeycloakTokenResponse(
        [property: JsonPropertyName("access_token")] string AccessToken,
        [property: JsonPropertyName("expires_in")] int ExpiresIn);
}

public sealed class ServiceAccessTokenHandler(IServiceAccessTokenProvider tokenProvider) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var token = await tokenProvider.GetAccessTokenAsync(cancellationToken);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return await base.SendAsync(request, cancellationToken);
    }
}
