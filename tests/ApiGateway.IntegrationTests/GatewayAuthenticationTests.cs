using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace ApiGateway.IntegrationTests;

public sealed class GatewayAuthenticationTests(GatewayTestFixture fixture)
    : IClassFixture<GatewayTestFixture>
{
    [Fact]
    public async Task Health_IsAnonymous_EvenWhenNoTokenIsProvided()
    {
        using var client = fixture.Gateway.CreateClient();

        using var response = await client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Healthy", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Health_RemainsAvailable_WhenInvalidTokenIsSent()
    {
        using var client = fixture.Gateway.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", "invalid-token-that-health-must-ignore");

        using var response = await client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Theory]
    [InlineData("/pet/pets/1")]
    [InlineData("/appointment/appointments/upcoming")]
    [InlineData("/treatment/api/treatments/pet/11111111-1111-1111-1111-111111111111")]
    public async Task ProxyRoutes_WithoutToken_ReturnUnauthorized(string path)
    {
        using var client = fixture.Gateway.CreateClient();

        using var response = await client.GetAsync(path);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Contains("Bearer", response.Headers.WwwAuthenticate.Select(value => value.Scheme));
    }

    [Fact]
    public async Task ProxyRoute_WithMalformedToken_ReturnsUnauthorized()
    {
        using var client = fixture.Gateway.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "not-a-jwt");

        using var response = await client.GetAsync("/treatment/api/treatments/pet/1");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task RejectedRequest_IsNotForwardedToDownstreamService()
    {
        using var client = fixture.Gateway.CreateClient();
        var requestCountBefore = fixture.Treatment.Requests.Count;

        using var response = await client.PostAsync(
            "/treatment/api/treatments",
            JsonContent.Create(new { diagnosis = "must not be forwarded" }));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal(requestCountBefore, fixture.Treatment.Requests.Count);
    }

    [Theory]
    [MemberData(nameof(InvalidTokens))]
    public async Task ProxyRoute_WithInvalidJwt_ReturnsUnauthorized(string token)
    {
        using var client = fixture.Gateway.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        using var response = await client.GetAsync("/treatment/api/treatments/pet/1");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ValidAuthenticatedUser_DoesNotNeedAGatewaySpecificRole()
    {
        using var client = fixture.CreateAuthenticatedClient(role: "owner");

        using var response = await client.GetAsync("/treatment/api/treatments/pet/1");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task ValidTokenWithoutRole_IsStillAuthenticatedAtGateway()
    {
        using var client = fixture.Gateway.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            GatewayTestFixture.CreateToken(role: null));

        using var response = await client.GetAsync("/pet/pets/1");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task UnknownRoute_ReturnsNotFound_InsteadOfBeingForwarded()
    {
        using var client = fixture.CreateAuthenticatedClient();

        using var response = await client.GetAsync("/not-a-configured-route");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    public static IEnumerable<object[]> InvalidTokens()
    {
        yield return
        [
            GatewayTestFixture.CreateToken(
                issuer: "wrong-issuer")
        ];
        yield return
        [
            GatewayTestFixture.CreateToken(
                audience: "wrong-audience")
        ];
        yield return
        [
            GatewayTestFixture.CreateToken(
                signingKey: "different-signing-key-that-is-at-least-32-bytes!!")
        ];
        yield return
        [
            GatewayTestFixture.CreateToken(
                expires: DateTime.UtcNow.AddMinutes(-10))
        ];
    }
}
