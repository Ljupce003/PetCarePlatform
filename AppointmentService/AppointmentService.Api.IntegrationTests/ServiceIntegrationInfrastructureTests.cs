using System.Net;
using System.Text;
using AppointmentService.Infrastructure.Security;
using Microsoft.Extensions.Options;
using Shared.ServiceDiscovery;
using Xunit;

namespace AppointmentService.Api.IntegrationTests;

public sealed class ServiceIntegrationInfrastructureTests
{
    [Fact]
    public async Task KeycloakProvider_UsesClientCredentialsAndCachesTheToken()
    {
        var handler = new TokenEndpointHandler();
        var client = new HttpClient(handler) { BaseAddress = new Uri("http://keycloak/realms/petcare/") };
        var provider = new KeycloakServiceAccessTokenProvider(
            new SingleClientFactory(client),
            Options.Create(new KeycloakServiceAccessTokenOptions
            {
                ClientId = "appointment-service",
                ClientSecret = "appointment-secret",
                RefreshSkewSeconds = 30
            }),
            TimeProvider.System);

        var first = await provider.GetAccessTokenAsync(CancellationToken.None);
        var second = await provider.GetAccessTokenAsync(CancellationToken.None);

        Assert.Equal("keycloak-service-token", first);
        Assert.Equal(first, second);
        Assert.Equal(1, handler.CallCount);
        Assert.Contains("grant_type=client_credentials", handler.LastRequestBody);
        Assert.Contains("client_id=appointment-service", handler.LastRequestBody);
        Assert.Contains("client_secret=appointment-secret", handler.LastRequestBody);
    }

    [Fact]
    public async Task DiscoveryHandler_RewritesTheLogicalPetHostToTheHealthyConsulInstance()
    {
        var capture = new RequestCaptureHandler();
        var discovery = new ServiceDiscoveryHandler(
            new FixedResolver(new Uri("http://pet-service-instance:8080/")))
        {
            InnerHandler = capture
        };
        using var client = new HttpClient(discovery);

        using var response = await client.GetAsync(
            "http://pet-service/api/pets/44444444-4444-4444-4444-444444444444/exists" +
            "?ownerId=33333333-3333-3333-3333-333333333333");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(
            "http://pet-service-instance:8080/api/pets/44444444-4444-4444-4444-444444444444/exists" +
            "?ownerId=33333333-3333-3333-3333-333333333333",
            capture.RequestUri?.AbsoluteUri);
    }

    private sealed class TokenEndpointHandler : HttpMessageHandler
    {
        public int CallCount { get; private set; }
        public string LastRequestBody { get; private set; } = string.Empty;

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            CallCount++;
            LastRequestBody = await request.Content!.ReadAsStringAsync(cancellationToken);
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    "{\"access_token\":\"keycloak-service-token\",\"expires_in\":300}",
                    Encoding.UTF8,
                    "application/json")
            };
        }
    }

    private sealed class RequestCaptureHandler : HttpMessageHandler
    {
        public Uri? RequestUri { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            RequestUri = request.RequestUri;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        }
    }

    private sealed class SingleClientFactory(HttpClient client) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => client;
    }

    private sealed class FixedResolver(Uri result) : IConsulServiceResolver
    {
        public Task<Uri> ResolveAsync(string serviceName, CancellationToken cancellationToken = default) =>
            Task.FromResult(result);

        public Task<IReadOnlyList<Uri>> ResolveAllAsync(
            string serviceName,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Uri>>([result]);
    }
}
