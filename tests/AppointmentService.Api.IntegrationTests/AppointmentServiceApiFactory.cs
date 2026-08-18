using System.Net.Http.Headers;
using System.Net.Http.Json;
using AppointmentService.Application.Abstractions;
using AppointmentService.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Shared.Messaging;

namespace AppointmentService.Api.IntegrationTests;

/// <summary>
/// Boots the real Api/Application/Infrastructure composition (real controllers, real
/// [Authorize]/role checks, real domain rules) against an EF Core InMemory database instead of
/// Postgres, and swaps the Kafka publisher for an in-memory spy -- so the HTTP pipeline is
/// exercised without needing Docker/Postgres/Kafka running.
///
/// Runs under the "Testing" environment specifically (see <see cref="ConfigureWebHost"/>): CI has
/// no live Keycloak to reach, so this is the one environment Program.cs's JWT bearer setup and
/// AuthController.Login validate/issue a locally-signed token for instead of going through
/// Keycloak -- every other environment (local `dotnet run`, Docker, production) is Keycloak-only.
/// Pet ownership and Kafka are replaced only inside this test host.
/// </summary>
public sealed class AppointmentServiceApiFactory : WebApplicationFactory<Program>
{
    private readonly string _databaseName = $"appointment-service-tests-{Guid.NewGuid()}";

    public FakeIntegrationEventPublisher Events => Services.GetRequiredService<FakeIntegrationEventPublisher>();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IPetVerificationClient>();
            services.AddSingleton<IPetVerificationClient, TestPetVerificationClient>();

            // Program.cs's own AddDbContext<AppointmentDbContext>(UseNpgsql) already ran by this
            // point and left Npgsql's provider services in this same IServiceCollection --
            // RemoveAll<DbContextOptions<...>>() only removes the *options* registration, not
            // those provider services, so EF's shared internal service provider ends up seeing
            // both Npgsql's and InMemory's IDatabaseProvider side by side and refuses to pick one
            // ("Only a single database provider can be registered..."). Giving the InMemory
            // registration its own dedicated internal service provider sidesteps the shared
            // collection entirely instead of trying to unregister Npgsql's leftovers.
            services.RemoveAll<DbContextOptions<AppointmentDbContext>>();
            var inMemoryProvider = new ServiceCollection()
                .AddEntityFrameworkInMemoryDatabase()
                .BuildServiceProvider();
            services.AddDbContext<AppointmentDbContext>(options =>
            {
                options.UseInMemoryDatabase(_databaseName);
                options.UseInternalServiceProvider(inMemoryProvider);
            });

            services.RemoveAll<IIntegrationEventPublisher>();
            services.AddSingleton<FakeIntegrationEventPublisher>();
            services.AddSingleton<IIntegrationEventPublisher>(provider => provider.GetRequiredService<FakeIntegrationEventPublisher>());

            // Consul is unreachable in tests. ConsulRegistrationHostedService already degrades
            // gracefully (logs a warning instead of throwing), but without this the "consul"
            // HttpClient's default 100s timeout would make every test run painfully slow.
            services.AddHttpClient("consul").ConfigureHttpClient(client => client.Timeout = TimeSpan.FromMilliseconds(500));

            // Program.cs's own startup seeding calls Database.MigrateAsync(), which the InMemory
            // provider doesn't support -- it logs a warning and silently no-ops for this host, so
            // this hosted service does the InMemory-compatible equivalent (EnsureCreated + seed).
            services.AddHostedService<TestDataSeeder>();
        });
    }

    /// <summary>Logs in as one of the demo users (owner1/vet1/admin1) via the "Testing"-environment local token (see <see cref="TestUsers"/>) and returns a client with it attached.</summary>
    public async Task<HttpClient> CreateAuthenticatedClientAsync(string username, string password)
    {
        var client = CreateClient();
        var response = await client.PostAsJsonAsync("/auth/login", new { username, password });
        response.EnsureSuccessStatusCode();

        var token = await response.Content.ReadFromJsonAsync<TokenResponsePayload>(JsonDefaults.CaseInsensitive);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token!.AccessToken);
        return client;
    }

    private sealed record TokenResponsePayload(string AccessToken, string Role, Guid? UserId);

    private sealed class TestPetVerificationClient : IPetVerificationClient
    {
        public Task<PetVerificationResult> VerifyAsync(
            Guid petId,
            Guid ownerId,
            CancellationToken cancellationToken) =>
            Task.FromResult(new PetVerificationResult(petId != Guid.Empty, ownerId != Guid.Empty));
    }
}
