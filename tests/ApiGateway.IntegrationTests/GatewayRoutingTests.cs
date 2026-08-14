using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace ApiGateway.IntegrationTests;

public sealed class GatewayRoutingTests(GatewayTestFixture fixture)
    : IClassFixture<GatewayTestFixture>
{
    [Theory]
    [InlineData("/pet/pets/42?include=owner", "pet", "/pets/42", "?include=owner")]
    [InlineData("/appointment/appointments/upcoming?ownerId=7", "appointment", "/appointments/upcoming", "?ownerId=7")]
    [InlineData("/treatment/api/treatments/pet/9?limit=5", "treatment", "/api/treatments/pet/9", "?limit=5")]
    public async Task ServiceRoutes_ReachCorrectCluster_AndRemovePublicPrefix(
        string publicPath,
        string service,
        string downstreamPath,
        string query)
    {
        using var client = fixture.CreateAuthenticatedClient();

        using var response = await client.GetAsync(publicPath);
        var echo = await response.Content.ReadFromJsonAsync<DownstreamEcho>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(echo);
        Assert.Equal(service, echo.Service);
        Assert.Equal(downstreamPath, echo.Path);
        Assert.Equal(query, echo.Query);
    }

    [Fact]
    public async Task AuthorizationHeader_IsForwardedUnchanged()
    {
        var token = GatewayTestFixture.CreateToken(role: "veterinarian");
        using var client = fixture.Gateway.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        using var response = await client.GetAsync("/treatment/api/treatments/pet/9");
        var echo = await response.Content.ReadFromJsonAsync<DownstreamEcho>();

        Assert.Equal($"Bearer {token}", echo?.Authorization);
    }

    [Fact]
    public async Task Post_ForwardsMethodJsonBodyAndCorrelationHeader()
    {
        using var client = fixture.CreateAuthenticatedClient(role: "veterinarian");
        const string json = """{"petId":"11111111-1111-1111-1111-111111111111","diagnosis":"Healthy"}""";
        using var request = new HttpRequestMessage(HttpMethod.Post, "/treatment/api/treatments")
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
        request.Headers.Add("X-Correlation-ID", "gateway-correlation-123");

        using var response = await client.SendAsync(request);
        var echo = await response.Content.ReadFromJsonAsync<DownstreamEcho>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(echo);
        Assert.Equal("POST", echo.Method);
        Assert.Equal("/api/treatments", echo.Path);
        Assert.Equal("gateway-correlation-123", echo.CorrelationId);
        Assert.True(JsonElement.DeepEquals(
            JsonSerializer.Deserialize<JsonElement>(json),
            JsonSerializer.Deserialize<JsonElement>(echo.Body)));
    }

    [Fact]
    public async Task DownstreamErrorStatusBodyContentTypeAndHeader_ArePreserved()
    {
        using var client = fixture.CreateAuthenticatedClient();

        using var response = await client.GetAsync("/treatment/status/teapot");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal((HttpStatusCode)418, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal("treatment", response.Headers.GetValues("X-Downstream-Error").Single());
        Assert.Contains("\"error\":\"teapot\"", body);
    }

    [Fact]
    public async Task McpRoute_PreservesPathProtocolHeaderAndStreamableHttpResponse()
    {
        using var client = fixture.CreateAuthenticatedClient(role: "veterinarian");
        using var request = new HttpRequestMessage(HttpMethod.Post, "/mcp")
        {
            Content = new StringContent(
                """{"jsonrpc":"2.0","id":1,"method":"tools/list","params":{}}""",
                Encoding.UTF8,
                "application/json")
        };
        request.Headers.TryAddWithoutValidation("MCP-Protocol-Version", "2025-11-25");
        request.Headers.TryAddWithoutValidation("Accept", "application/json, text/event-stream");

        using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/event-stream", response.Content.Headers.ContentType?.MediaType);
        Assert.Contains("no-cache", response.Headers.CacheControl?.ToString());
        Assert.Equal("no", response.Headers.GetValues("X-Accel-Buffering").Single());
        Assert.Contains("event: message", body);
        Assert.Contains("\"jsonrpc\":\"2.0\"", body);

        var captured = fixture.Mcp.Requests.Last();
        Assert.Equal("/mcp", captured.Path);
        Assert.Equal("2025-11-25", captured.Header("MCP-Protocol-Version"));
        Assert.StartsWith("Bearer ", captured.Header("Authorization"));
    }

    [Fact]
    public async Task UnavailableDestination_ReturnsBadGateway()
    {
        await using var gateway = new GatewayFactory(new GatewayDestinations(
            fixture.Pet.Address,
            fixture.Appointment.Address,
            "http://127.0.0.1:1/",
            fixture.Mcp.Address));
        using var client = gateway.CreateClient();
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(
            "Bearer",
            GatewayTestFixture.CreateToken());

        using var response = await client.GetAsync("/treatment/api/treatments/pet/9");

        Assert.Equal(HttpStatusCode.BadGateway, response.StatusCode);
    }
}
