using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Xunit;

namespace TreatmentAndNotificationService.Api.IntegrationTests;

[Collection(PostgreSqlCollection.Name)]
public sealed class TreatmentAuthorizationTests : IAsyncLifetime
{
    private static readonly Guid PetId = Guid.Parse("a1111111-1111-1111-1111-111111111111");
    private static readonly Guid OwnerId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private readonly TreatmentApiFactory _factory;
    private HttpClient _anonymousClient = null!;

    public TreatmentAuthorizationTests(PostgreSqlFixture database) =>
        _factory = new TreatmentApiFactory(database.ConnectionString);

    public async Task InitializeAsync()
    {
        _anonymousClient = _factory.CreateClient();
        await _factory.ResetDatabaseAsync();
    }

    public async Task DisposeAsync()
    {
        _anonymousClient.Dispose();
        await _factory.DisposeAsync();
    }

    [Theory]
    [InlineData("/api/treatments/pet/a1111111-1111-1111-1111-111111111111")]
    [InlineData("/api/vaccinations/pet/a1111111-1111-1111-1111-111111111111")]
    [InlineData("/api/notifications/owner/11111111-1111-1111-1111-111111111111")]
    public async Task ProtectedRead_WithoutToken_Returns401(string path)
    {
        var response = await _anonymousClient.GetAsync(path);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task MedicalRead_WithOwnerToken_Returns200()
    {
        using var ownerClient = _factory.CreateAuthenticatedClient("owner");

        var response = await ownerClient.GetAsync($"/api/treatments/pet/{PetId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Theory]
    [InlineData("owner")]
    [InlineData("service")]
    public async Task RecordExamination_WithoutVeterinarianOrAdminRole_Returns403(string role)
    {
        using var client = _factory.CreateAuthenticatedClient(role);

        var response = await client.PostAsJsonAsync("/api/treatments", new { });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Theory]
    [InlineData("veterinarian")]
    [InlineData("admin")]
    public async Task RecordExamination_WithAuthorizedRole_ReachesValidation(string role)
    {
        using var client = _factory.CreateAuthenticatedClient(role);

        var response = await client.PostAsJsonAsync("/api/treatments", new { });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task RecordVaccination_WithOwnerRole_Returns403()
    {
        using var ownerClient = _factory.CreateAuthenticatedClient("owner");

        var response = await ownerClient.PostAsJsonAsync("/api/vaccinations", new { });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task CreateNotification_WithOwnerRole_Returns403()
    {
        using var ownerClient = _factory.CreateAuthenticatedClient("owner");

        var response = await ownerClient.PostAsJsonAsync("/api/notifications", new { });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task CreateNotification_WithServiceRole_ReachesValidation()
    {
        using var serviceClient = _factory.CreateAuthenticatedClient("service");

        var response = await serviceClient.PostAsJsonAsync("/api/notifications", new { });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task OwnerNotifications_WithServiceRole_Returns200()
    {
        using var serviceClient = _factory.CreateAuthenticatedClient("service");

        var response = await serviceClient.GetAsync($"/api/notifications/owner/{OwnerId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task InvalidToken_Returns401()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "not-a-jwt");

        var response = await client.GetAsync($"/api/treatments/pet/{PetId}");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task HealthAndOpenApi_AreAnonymousAndOpenApiDeclaresBearerScheme()
    {
        var health = await _anonymousClient.GetAsync("/health");
        var openApi = await _anonymousClient.GetAsync("/openapi/v1.json");
        var openApiJson = await openApi.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, health.StatusCode);
        Assert.Equal(HttpStatusCode.OK, openApi.StatusCode);
        Assert.Contains("Bearer", openApiJson);
        Assert.Contains("bearer", openApiJson, StringComparison.OrdinalIgnoreCase);
    }
}
