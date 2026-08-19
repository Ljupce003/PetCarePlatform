using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PetService.Domain.Entities;
using PetService.Domain.Enums;
using PetService.Infrastructure.Persistence;

namespace PetService.Api.IntegrationTests;

public sealed class PetServiceApiFactory : WebApplicationFactory<Program>
{
    public static readonly Guid OwnerId = new("33333333-3333-3333-3333-333333333333");
    public static readonly Guid DifferentOwnerId = new("33333333-3333-3333-3333-333333333334");
    public static readonly Guid PetId = new("44444444-4444-4444-4444-444444444444");

    private readonly string _databaseName = $"pet-service-tests-{Guid.NewGuid():N}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<PetDbContext>>();
            var inMemoryProvider = new ServiceCollection()
                .AddEntityFrameworkInMemoryDatabase()
                .BuildServiceProvider();
            services.AddDbContext<PetDbContext>(options =>
            {
                options.UseInMemoryDatabase(_databaseName);
                options.UseInternalServiceProvider(inMemoryProvider);
            });

            // Consul is an external dependency and is not part of an in-process API test.
            services.RemoveAll<IHostedService>();

            services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = TestAuthenticationHandler.TestScheme;
                    options.DefaultChallengeScheme = TestAuthenticationHandler.TestScheme;
                    options.DefaultForbidScheme = TestAuthenticationHandler.TestScheme;
                })
                .AddScheme<AuthenticationSchemeOptions, TestAuthenticationHandler>(
                    TestAuthenticationHandler.TestScheme,
                    _ => { });
        });
    }

    public HttpClient CreateAuthenticatedClient(string role, Guid? subjectId = null)
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthenticationHandler.RoleHeader, role);
        if (subjectId is not null)
            client.DefaultRequestHeaders.Add(TestAuthenticationHandler.SubjectHeader, subjectId.Value.ToString());
        return client;
    }

    public async Task ResetDatabaseAsync(bool seedDemoData = true)
    {
        await ResetDatabaseAsync(seedDemoData
            ? PetDatabaseScenario.OwnedByRequestedOwner
            : PetDatabaseScenario.Empty);
    }

    public async Task ResetDatabaseAsync(PetDatabaseScenario scenario)
    {
        await using var scope = Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PetDbContext>();
        await dbContext.Database.EnsureDeletedAsync();
        await dbContext.Database.EnsureCreatedAsync();

        if (scenario == PetDatabaseScenario.Empty)
        {
            return;
        }

        var owner = Owner.Seed(OwnerId, "Contract Test Owner", "contract.owner@example.com", "+38970123456", "Skopje");
        var differentOwner = Owner.Seed(DifferentOwnerId, "Different Test Owner", "different.owner@example.com", "+38970987654", "Skopje");
        var pet = Pet.Seed(
            PetId,
            scenario == PetDatabaseScenario.OwnedByRequestedOwner ? OwnerId : DifferentOwnerId,
            "Luna",
            PetSpecies.Dog,
            "Labrador",
            new DateOnly(2021, 5, 12),
            27.5m,
            "MKD000000001");

        dbContext.Owners.AddRange(owner, differentOwner);
        dbContext.Pets.Add(pet);
        await dbContext.SaveChangesAsync();
    }
}

public enum PetDatabaseScenario
{
    Empty,
    OwnedByRequestedOwner,
    OwnedByDifferentOwner
}

internal sealed class TestAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string TestScheme = "PetServiceTests";
    public const string RoleHeader = "X-Test-Role";
    public const string SubjectHeader = "X-Test-Subject";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(RoleHeader, out var role) || string.IsNullOrWhiteSpace(role))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var subject = Request.Headers.TryGetValue(SubjectHeader, out var subjectHeader)
            ? subjectHeader.ToString()
            : $"{role}-test-user";
        var identity = new ClaimsIdentity(
            [
                new Claim(ClaimTypes.NameIdentifier, subject),
                new Claim("sub", subject),
                new Claim(ClaimTypes.Role, role.ToString())
            ],
            TestScheme);
        return Task.FromResult(AuthenticateResult.Success(
            new AuthenticationTicket(new ClaimsPrincipal(identity), TestScheme)));
    }
}
