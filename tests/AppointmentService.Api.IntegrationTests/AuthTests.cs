using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using AppointmentService.Infrastructure.Persistence;
using Xunit;

namespace AppointmentService.Api.IntegrationTests;

public sealed class AuthTests(AppointmentServiceApiFactory factory) : IClassFixture<AppointmentServiceApiFactory>
{
    [Theory]
    [InlineData("owner1", "Owner123!", "owner")]
    [InlineData("vet1", "Vet123!", "veterinarian")]
    [InlineData("admin1", "Admin123!", "admin")]
    public async Task Login_WithValidCredentials_ReturnsATokenForTheExpectedRole(string username, string password, string expectedRole)
    {
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/auth/login", new { username, password });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;
        Assert.False(string.IsNullOrWhiteSpace(body.GetProperty("accessToken").GetString()));
        Assert.Equal(expectedRole, body.GetProperty("role").GetString());
    }

    [Fact]
    public async Task Login_AsOwner_ReturnsTheSameUserIdAsTheSeededDemoOwner()
    {
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/auth/login", new { username = "owner1", password = "Owner123!" });

        var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;
        Assert.Equal(AppointmentDbInitializer.DemoOwnerId.ToString(), body.GetProperty("userId").GetString());
    }

    [Fact]
    public async Task Login_WithWrongPassword_ReturnsUnauthorized()
    {
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/auth/login", new { username = "owner1", password = "wrong" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_WithUnknownUsername_ReturnsUnauthorized()
    {
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/auth/login", new { username = "nobody", password = "irrelevant" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
