using System.Net;
using System.Text.Json;
using Xunit;

namespace AppointmentService.Api.IntegrationTests;

public sealed class HealthEndpointTests(AppointmentServiceApiFactory factory) : IClassFixture<AppointmentServiceApiFactory>
{
    [Fact]
    public async Task Health_ReturnsOkWithStatusAndChecks()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("Healthy", body.RootElement.GetProperty("status").GetString());
        Assert.True(body.RootElement.GetProperty("checks").GetArrayLength() > 0);
    }

    [Fact]
    public async Task HealthLive_ReturnsOkWithoutRunningTheDatabaseCheck()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/health/live");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        // Predicate = _ => false in Program.cs means no registered check actually runs here.
        Assert.Equal(0, body.RootElement.GetProperty("checks").GetArrayLength());
    }

    [Fact]
    public async Task Health_DoesNotRequireAuthentication()
    {
        var client = factory.CreateClient(); // no Authorization header

        var response = await client.GetAsync("/health");

        Assert.NotEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
