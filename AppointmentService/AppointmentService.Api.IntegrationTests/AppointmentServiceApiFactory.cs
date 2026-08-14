using System.Net.Http.Headers;
using System.Net.Http.Json;
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
/// exercised without needing Docker/Postgres/Kafka running. Pet-ownership verification uses
/// FakePetVerificationClient automatically, the same way it does for local `dotnet run`, since
/// this factory doesn't change the environment from "Development" (see
/// appsettings.Development.json's PetService:UseFakeVerification).
///
/// JWT auth is the one dependency this can't fake away: both token validation (Program.cs) and
/// <c>POST /auth/login</c> (AuthController) always go through the real Keycloak realm now, so
/// <c>Jwt:Authority</c> (default: http://localhost:8080/realms/petcare) must be reachable for
/// these tests to pass -- run `docker compose up keycloak` first.
/// </summary>
public sealed class AppointmentServiceApiFactory : WebApplicationFactory<Program>
{
    private readonly string _databaseName = $"appointment-service-tests-{Guid.NewGuid()}";

    public FakeIntegrationEventPublisher Events => Services.GetRequiredService<FakeIntegrationEventPublisher>();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<AppointmentDbContext>>();
            services.AddDbContext<AppointmentDbContext>(options => options.UseInMemoryDatabase(_databaseName));

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

    /// <summary>Logs in as one of the demo users (owner1/vet1/admin1) against real Keycloak and returns a client with the resulting token attached.</summary>
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
}
