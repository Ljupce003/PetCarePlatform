using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using PactNet.Verifier;

namespace PetService.Api.IntegrationTests;

public sealed class PetProviderPactTests(PetServiceApiFactory factory) : IClassFixture<PetServiceApiFactory>
{
    [Theory]
    [InlineData(
        "a request to verify a pet that exists and belongs to the given owner",
        PetDatabaseScenario.OwnedByRequestedOwner)]
    [InlineData(
        "a request to verify a pet that exists but does not belong to the given owner",
        PetDatabaseScenario.OwnedByDifferentOwner)]
    [InlineData(
        "a request to verify a pet that does not exist",
        PetDatabaseScenario.Empty)]
    public async Task PetService_HonorsAppointmentConsumerInteraction(
        string interactionDescription,
        PetDatabaseScenario scenario)
    {
        // Appointment's checked-in consumer pact does not declare provider states and reuses the
        // same IDs for three outcomes. Verify each interaction against its required provider data
        // without changing the Appointment-owned pact.
        await factory.ResetDatabaseAsync(scenario);
        using var testServerClient = factory.CreateAuthenticatedClient("service");
        await using var proxy = await TestServerProxy.StartAsync(testServerClient);

        using var verifier = new PactVerifier("Pet Service");
        verifier
            .WithHttpEndpoint(proxy.BaseAddress)
            .WithFileSource(new FileInfo(Path.Combine(
                FindRepositoryRoot().FullName,
                "tests",
                "Contracts",
                "pacts",
                "Appointment Service-Pet Service.json")))
            .WithFilter(interactionDescription, null!)
            .Verify();
    }

    private static DirectoryInfo FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "PetCarePlatform.slnx")))
        {
            directory = directory.Parent;
        }

        return directory ?? throw new DirectoryNotFoundException("Repository root was not found.");
    }

    /// <summary>
    /// Pact's native verifier needs a TCP endpoint, while WebApplicationFactory uses an in-memory
    /// TestServer. This tiny loopback proxy forwards verifier requests into the real ASP.NET Core
    /// test pipeline, including routing, authorization, handlers, and EF persistence.
    /// </summary>
    private sealed class TestServerProxy : IAsyncDisposable
    {
        private readonly WebApplication _proxy;

        private TestServerProxy(WebApplication proxy, Uri baseAddress)
        {
            _proxy = proxy;
            BaseAddress = baseAddress;
        }

        public Uri BaseAddress { get; }

        public static async Task<TestServerProxy> StartAsync(HttpClient target)
        {
            var builder = WebApplication.CreateSlimBuilder();
            builder.WebHost.UseUrls("http://127.0.0.1:0");
            var proxy = builder.Build();
            proxy.Run(async context =>
            {
                using var request = new HttpRequestMessage(
                    new HttpMethod(context.Request.Method),
                    context.Request.Path + context.Request.QueryString);
                using var response = await target.SendAsync(request, context.RequestAborted);
                var body = await response.Content.ReadAsByteArrayAsync(context.RequestAborted);

                context.Response.StatusCode = (int)response.StatusCode;
                if (response.Content.Headers.ContentType is not null)
                {
                    context.Response.ContentType = response.Content.Headers.ContentType.ToString();
                }
                context.Response.ContentLength = body.Length;
                await context.Response.Body.WriteAsync(body, context.RequestAborted);
            });
            await proxy.StartAsync();

            var addresses = proxy.Services.GetRequiredService<IServer>()
                .Features.Get<IServerAddressesFeature>()?.Addresses;
            var baseAddress = new Uri(addresses?.Single()
                ?? throw new InvalidOperationException("The Pact proxy did not publish an address."));
            return new TestServerProxy(proxy, baseAddress);
        }

        public async ValueTask DisposeAsync()
        {
            await _proxy.StopAsync();
            await _proxy.DisposeAsync();
        }
    }
}
