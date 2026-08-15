using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using PetService.Application.Dtos;
using PetService.Domain.Enums;

namespace PetService.Api.IntegrationTests;

public sealed class PetApiTests(PetServiceApiFactory factory) : IClassFixture<PetServiceApiFactory>
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    [Fact]
    public async Task OwnerAndPetCrudWorkflow_UsesTheRealApiApplicationAndPersistenceLayers()
    {
        await factory.ResetDatabaseAsync(seedDemoData: false);
        using var client = factory.CreateAuthenticatedClient("owner");

        var ownerResponse = await client.PostAsJsonAsync("/owners", new
        {
            ownerName = "Test Owner",
            email = "test.owner@example.com",
            phone = "+389711222333",
            address = "Skopje"
        });
        Assert.Equal(HttpStatusCode.Created, ownerResponse.StatusCode);
        var owner = await ownerResponse.Content.ReadFromJsonAsync<OwnerDto>(Json);
        Assert.NotNull(owner);

        var petResponse = await client.PostAsJsonAsync("/pets", new
        {
            ownerId = owner.OwnerId,
            name = "Luna",
            species = "Dog",
            breed = "Labrador",
            birthDate = "2021-05-12",
            weight = 27.5m,
            microchipNumber = "MKD000000001",
            allergies = new[] { "Chicken" },
            chronicConditions = Array.Empty<string>()
        });
        Assert.Equal(HttpStatusCode.Created, petResponse.StatusCode);
        var pet = await petResponse.Content.ReadFromJsonAsync<PetDto>(Json);
        Assert.NotNull(pet);

        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync($"/owners/{owner.OwnerId}")) .StatusCode);
        Assert.Single(await client.GetFromJsonAsync<List<OwnerDto>>("/owners", Json) ?? []);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync($"/pets/{pet.PetId}")).StatusCode);
        Assert.Single(await client.GetFromJsonAsync<List<PetDto>>("/pets", Json) ?? []);
        Assert.Single(await client.GetFromJsonAsync<List<PetDto>>($"/owners/{owner.OwnerId}/pets", Json) ?? []);

        var updatedPet = await client.PutAsJsonAsync($"/pets/{pet.PetId}", new
        {
            name = "Luna II",
            species = "Dog",
            breed = "Labrador Retriever",
            birthDate = "2021-05-12",
            weight = 28m,
            microchipNumber = "MKD000000001",
            allergies = Array.Empty<string>(),
            chronicConditions = Array.Empty<string>()
        });
        Assert.Equal(HttpStatusCode.OK, updatedPet.StatusCode);
        Assert.Equal("Luna II", (await updatedPet.Content.ReadFromJsonAsync<PetDto>(Json))?.Name);

        var updatedOwner = await client.PutAsJsonAsync($"/owners/{owner.OwnerId}", new
        {
            ownerName = "Updated Test Owner",
            email = "test.owner@example.com",
            phone = "+38970123456",
            address = "Bitola"
        });
        Assert.Equal(HttpStatusCode.OK, updatedOwner.StatusCode);
        Assert.Equal("Bitola", (await updatedOwner.Content.ReadFromJsonAsync<OwnerDto>(Json))?.Address);

        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync($"/pets/{pet.PetId}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/pets/{pet.PetId}")).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync($"/owners/{owner.OwnerId}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/owners/{owner.OwnerId}")).StatusCode);
    }

    [Fact]
    public async Task ValidationAndMissingOwner_ReturnMeaningfulClientErrors()
    {
        await factory.ResetDatabaseAsync(seedDemoData: false);
        using var client = factory.CreateAuthenticatedClient("owner");

        var invalidOwner = await client.PostAsJsonAsync("/owners", new
        {
            ownerName = "",
            email = "not-email",
            phone = "123",
            address = (string?)null
        });
        Assert.Equal(HttpStatusCode.BadRequest, invalidOwner.StatusCode);

        var missingOwnerPet = await client.PostAsJsonAsync("/pets", ValidPetRequest(Guid.NewGuid()));
        Assert.Equal(HttpStatusCode.NotFound, missingOwnerPet.StatusCode);

        var invalidPet = await client.PostAsJsonAsync("/pets", ValidPetRequest(Guid.NewGuid()) with
        {
            Name = "",
            Weight = 0,
            BirthDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1))
        });
        Assert.Equal(HttpStatusCode.BadRequest, invalidPet.StatusCode);
    }

    [Fact]
    public async Task OwnershipContract_ReturnsOnlyExistsAndOwnedByOwner()
    {
        await factory.ResetDatabaseAsync();
        using var client = factory.CreateAuthenticatedClient("service");

        var owned = await client.GetAsync($"/pets/{PetServiceApiFactory.PetId}/exists?ownerId={PetServiceApiFactory.OwnerId}");
        Assert.Equal(HttpStatusCode.OK, owned.StatusCode);
        using var ownedJson = JsonDocument.Parse(await owned.Content.ReadAsStringAsync());
        Assert.Equal(2, ownedJson.RootElement.EnumerateObject().Count());
        Assert.True(ownedJson.RootElement.GetProperty("exists").GetBoolean());
        Assert.True(ownedJson.RootElement.GetProperty("ownedByOwner").GetBoolean());

        var compatibilityRoute = await client.GetAsync(
            $"/api/pets/{PetServiceApiFactory.PetId}/exists?ownerId={PetServiceApiFactory.OwnerId}");
        Assert.Equal(HttpStatusCode.OK, compatibilityRoute.StatusCode);

        var notOwned = await client.GetFromJsonAsync<PetOwnershipDto>(
            $"/pets/{PetServiceApiFactory.PetId}/exists?ownerId={PetServiceApiFactory.DifferentOwnerId}", Json);
        Assert.NotNull(notOwned);
        Assert.True(notOwned.Exists);
        Assert.False(notOwned.OwnedByOwner);

        var missing = await client.GetAsync($"/pets/{Guid.NewGuid()}/exists?ownerId={PetServiceApiFactory.OwnerId}");
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
    }

    [Fact]
    public async Task ProtectedEndpoints_EnforceAuthenticationAndRoles()
    {
        await factory.ResetDatabaseAsync();
        using var anonymous = factory.CreateClient();
        using var service = factory.CreateAuthenticatedClient("service");
        using var admin = factory.CreateAuthenticatedClient("admin");

        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync("/pets")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await service.GetAsync("/pets")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await admin.GetAsync("/pets")).StatusCode);
        Assert.Equal(
            HttpStatusCode.OK,
            (await service.GetAsync($"/pets/{PetServiceApiFactory.PetId}/exists?ownerId={PetServiceApiFactory.OwnerId}")).StatusCode);
    }

    [Fact]
    public async Task HealthAndOpenApi_AreAvailableWithoutAuthentication()
    {
        await factory.ResetDatabaseAsync();
        using var client = factory.CreateClient();

        var health = await client.GetAsync("/health");
        Assert.Equal(HttpStatusCode.OK, health.StatusCode);
        using var body = JsonDocument.Parse(await health.Content.ReadAsStringAsync());
        Assert.Equal("Healthy", body.RootElement.GetProperty("status").GetString());
        Assert.Contains(body.RootElement.GetProperty("checks").EnumerateArray(), check =>
            check.GetProperty("name").GetString() == "pet-database");

        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/health/live")).StatusCode);
        var openApi = await client.GetAsync("/openapi/v1.json");
        Assert.Equal(HttpStatusCode.OK, openApi.StatusCode);
        using var openApiDocument = JsonDocument.Parse(await openApi.Content.ReadAsStringAsync());
        var paths = openApiDocument.RootElement.GetProperty("paths");
        Assert.True(paths.TryGetProperty("/pets/{id}/exists", out _));
        Assert.True(paths.TryGetProperty("/api/pets/{id}/exists", out _));
    }

    private static PetRequest ValidPetRequest(Guid ownerId) => new(
        ownerId,
        "Luna",
        PetSpecies.Dog,
        "Labrador",
        new DateOnly(2021, 5, 12),
        27.5m,
        "MKD000000001",
        ["Chicken"],
        []);

    private sealed record PetRequest(
        Guid OwnerId,
        string Name,
        PetSpecies Species,
        string? Breed,
        DateOnly BirthDate,
        decimal Weight,
        string? MicrochipNumber,
        IReadOnlyList<string> Allergies,
        IReadOnlyList<string> ChronicConditions);
}
