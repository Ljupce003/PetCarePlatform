using System.Net;
using System.Text.Json;

namespace ApiGateway.IntegrationTests;

public sealed class GatewayOpenApiTests(GatewayTestFixture fixture)
    : IClassFixture<GatewayTestFixture>
{
    [Theory]
    [InlineData("pet", "pet", "/pet")]
    [InlineData("appointment", "appointment", "/appointment")]
    [InlineData("treatment", "treatment", "/treatment")]
    public async Task ServiceDocument_IsAnonymous_AndUsesGatewayServerPrefix(
        string documentId,
        string downstreamService,
        string expectedPrefix)
    {
        using var client = fixture.Gateway.CreateClient();

        using var response = await client.GetAsync($"/openapi/{documentId}.json");
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("3.1.0", document.RootElement.GetProperty("openapi").GetString());
        Assert.Equal(expectedPrefix,
            document.RootElement.GetProperty("servers")[0].GetProperty("url").GetString());
        Assert.Contains("via API Gateway",
            document.RootElement.GetProperty("info").GetProperty("title").GetString());
        Assert.True(document.RootElement.GetProperty("paths").TryGetProperty("/sample", out _));

        var downstream = downstreamService switch
        {
            "pet" => fixture.Pet,
            "appointment" => fixture.Appointment,
            "treatment" => fixture.Treatment,
            _ => throw new ArgumentOutOfRangeException(nameof(downstreamService))
        };
        var captured = downstream.Requests.Last(request => request.Path == "/openapi/v1.json");
        Assert.Null(captured.Header("Authorization"));
    }

    [Fact]
    public async Task SwaggerUi_IsAnonymous_AndListsAllServiceDocuments()
    {
        using var client = fixture.Gateway.CreateClient();

        using var response = await client.GetAsync("/swagger/index.html");
        var html = await response.Content.ReadAsStringAsync();
        using var configurationResponse = await client.GetAsync("/swagger/index.js");
        var configuration = await configurationResponse.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(HttpStatusCode.OK, configurationResponse.StatusCode);
        Assert.Contains("PetCare API Gateway Documentation", html);
        Assert.Contains("Pet Service", configuration);
        Assert.Contains("Appointment Service", configuration);
        Assert.Contains("Treatment", configuration);
        Assert.Contains("/openapi/pet.json", configuration);
        Assert.Contains("/openapi/appointment.json", configuration);
        Assert.Contains("/openapi/treatment.json", configuration);
    }

    [Fact]
    public async Task OpenApiCatalog_ListsRestServicesAndMcpTransportNote()
    {
        using var client = fixture.Gateway.CreateClient();

        using var response = await client.GetAsync("/openapi");
        var json = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("/swagger", json);
        Assert.Contains("/pet", json);
        Assert.Contains("/appointment", json);
        Assert.Contains("/treatment", json);
        Assert.Contains("JSON-RPC", json);
        Assert.Contains("/mcp", json);
    }

    [Fact]
    public async Task UnknownServiceDocument_ReturnsNotFound()
    {
        using var client = fixture.Gateway.CreateClient();

        using var response = await client.GetAsync("/openapi/unknown.json");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task UnavailableServiceDocument_ReturnsBadGateway()
    {
        await using var gateway = new GatewayFactory(new GatewayDestinations(
            fixture.Pet.Address,
            fixture.Appointment.Address,
            "http://127.0.0.1:1/",
            fixture.Mcp.Address));
        using var client = gateway.CreateClient();

        using var response = await client.GetAsync("/openapi/treatment.json");

        Assert.Equal(HttpStatusCode.BadGateway, response.StatusCode);
    }
}
