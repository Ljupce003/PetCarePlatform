using System.Net;
using Xunit;

namespace AppointmentService.Api.IntegrationTests;

/// <summary>
/// Doesn't mutate any booking state (only reads or gets rejected before reaching a handler), so
/// it's safe to share one factory/database across every test here.
/// </summary>
public sealed class AuthorizationTests(AppointmentServiceApiFactory factory) : IClassFixture<AppointmentServiceApiFactory>
{
    [Theory]
    [InlineData("/clinics")]
    [InlineData("/veterinarians")]
    [InlineData("/slots")]
    public async Task ProtectedGetEndpoint_WithoutToken_Returns401(string path)
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync(path);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [InlineData("/clinics")]
    [InlineData("/veterinarians")]
    [InlineData("/slots")]
    public async Task ProtectedGetEndpoint_AsAnyAuthenticatedRole_Returns200(string path)
    {
        var client = await factory.CreateAuthenticatedClientAsync("vet1", "Vet123!");

        var response = await client.GetAsync(path);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Health_And_Swagger_AreReachableWithoutOwnerOrAdminOrVetRole()
    {
        var client = factory.CreateClient();

        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/health")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/openapi/v1.json")).StatusCode);
    }
}
