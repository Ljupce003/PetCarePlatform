using System.Net;
using AppointmentService.Infrastructure.Clients;
using PactNet;
using Xunit;

namespace AppointmentService.PactTests;

/// <summary>
/// Consumer-side Pact tests for the contract PetServiceClient depends on (see the "Pet Service
/// contract this service depends on" section of AppointmentService/README.md):
/// <c>GET /api/pets/{petId}/exists?ownerId={ownerId}</c>. Running these regenerates the pact file
/// under /pacts, which Pet Service can (once it implements the endpoint) verify against on its
/// own side -- that half is Pet Service's own task list item, not this project's.
/// </summary>
public sealed class PetServiceConsumerPactTests
{
    private static readonly Guid PetId = Guid.Parse("44444444-4444-4444-4444-444444444444"); // = AppointmentDbInitializer.DemoPetId
    private static readonly Guid OwnerId = Guid.Parse("33333333-3333-3333-3333-333333333333"); // = AppointmentDbInitializer.DemoOwnerId

    private readonly IPactBuilderV4 _pactBuilder;

    public PetServiceConsumerPactTests()
    {
        var pact = Pact.V4("Appointment Service", "Pet Service", new PactConfig
        {
            PactDir = FindRepositoryRoot().Combine("pacts").FullName
        });
        _pactBuilder = pact.WithHttpInteractions();
    }

    [Fact]
    public async Task VerifyAsync_WhenPetExistsAndIsOwnedByOwner_ReturnsExistsAndOwned()
    {
        _pactBuilder
            .UponReceiving("a request to verify a pet that exists and belongs to the given owner")
            .WithRequest(HttpMethod.Get, $"/api/pets/{PetId}/exists")
            .WithQuery("ownerId", OwnerId.ToString())
            .WillRespond()
            .WithStatus(HttpStatusCode.OK)
            .WithHeader("Content-Type", "application/json; charset=utf-8")
            .WithJsonBody(new { exists = true, ownedByOwner = true });

        await _pactBuilder.VerifyAsync(async context =>
        {
            var httpClient = new HttpClient { BaseAddress = context.MockServerUri };
            var client = new PetServiceClient(httpClient);

            var result = await client.VerifyAsync(PetId, OwnerId, CancellationToken.None);

            Assert.True(result.Exists);
            Assert.True(result.IsOwnedByOwner);
        });
    }

    [Fact]
    public async Task VerifyAsync_WhenPetExistsButBelongsToADifferentOwner_ReturnsExistsButNotOwned()
    {
        _pactBuilder
            .UponReceiving("a request to verify a pet that exists but does not belong to the given owner")
            .WithRequest(HttpMethod.Get, $"/api/pets/{PetId}/exists")
            .WithQuery("ownerId", OwnerId.ToString())
            .WillRespond()
            .WithStatus(HttpStatusCode.OK)
            .WithHeader("Content-Type", "application/json; charset=utf-8")
            .WithJsonBody(new { exists = true, ownedByOwner = false });

        await _pactBuilder.VerifyAsync(async context =>
        {
            var httpClient = new HttpClient { BaseAddress = context.MockServerUri };
            var client = new PetServiceClient(httpClient);

            var result = await client.VerifyAsync(PetId, OwnerId, CancellationToken.None);

            Assert.True(result.Exists);
            Assert.False(result.IsOwnedByOwner);
        });
    }

    [Fact]
    public async Task VerifyAsync_WhenPetDoesNotExist_ReturnsNotExistsWithoutThrowing()
    {
        _pactBuilder
            .UponReceiving("a request to verify a pet that does not exist")
            .WithRequest(HttpMethod.Get, $"/api/pets/{PetId}/exists")
            .WithQuery("ownerId", OwnerId.ToString())
            .WillRespond()
            .WithStatus(HttpStatusCode.NotFound);

        await _pactBuilder.VerifyAsync(async context =>
        {
            var httpClient = new HttpClient { BaseAddress = context.MockServerUri };
            var client = new PetServiceClient(httpClient);

            var result = await client.VerifyAsync(PetId, OwnerId, CancellationToken.None);

            Assert.False(result.Exists);
            Assert.False(result.IsOwnedByOwner);
        });
    }

    private static DirectoryInfo FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "PetCarePlatform.slnx")))
        {
            directory = directory.Parent;
        }

        return directory ?? throw new DirectoryNotFoundException("Repository root (PetCarePlatform.slnx) was not found.");
    }
}

internal static class DirectoryInfoExtensions
{
    public static DirectoryInfo Combine(this DirectoryInfo directory, string child) => new(Path.Combine(directory.FullName, child));
}
