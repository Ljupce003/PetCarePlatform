using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Shared.ServiceDiscovery;

public sealed class ConsulOptions
{
    public string Address { get; set; } = "http://localhost:8500";
}

public sealed class ServiceRegistrationOptions
{
    public string Name { get; set; } = string.Empty;
    public string Id { get; set; } = string.Empty;
    public string Address { get; set; } = "localhost";
    public int Port { get; set; }
}

public interface IConsulServiceResolver
{
    Task<Uri> ResolveAsync(string serviceName, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Uri>> ResolveAllAsync(string serviceName, CancellationToken cancellationToken = default);
}

public sealed class ConsulServiceResolver(IHttpClientFactory httpClientFactory) : IConsulServiceResolver
{
    public async Task<Uri> ResolveAsync(string serviceName, CancellationToken cancellationToken = default)
    {
        var all = await ResolveAllAsync(serviceName, cancellationToken);
        if (all.Count == 0)
            throw new HttpRequestException($"No healthy instances of '{serviceName}' were found in Consul.");
        return all[Random.Shared.Next(all.Count)];
    }

    public async Task<IReadOnlyList<Uri>> ResolveAllAsync(string serviceName, CancellationToken cancellationToken = default)
    {
        var client = httpClientFactory.CreateClient("consul");
        using var response = await client.GetAsync(
            $"/v1/health/service/{Uri.EscapeDataString(serviceName)}?passing=true",
            cancellationToken);
        response.EnsureSuccessStatusCode();
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));

        var result = new List<Uri>();
        foreach (var item in document.RootElement.EnumerateArray())
        {
            var service = item.GetProperty("Service");
            var address = service.GetProperty("Address").GetString();
            var port = service.GetProperty("Port").GetInt32();
            if (!string.IsNullOrWhiteSpace(address) && port > 0)
                result.Add(new Uri($"http://{address}:{port}/"));
        }
        return result;
    }
}

/// <summary>
/// Rewrites outgoing requests aimed at a logical "*-service" host (e.g. http://pet-service/...)
/// to whatever address/port Consul currently reports as healthy for that service name. Requests
/// to any other host pass through untouched, so this can sit in every outgoing HttpClient's
/// handler pipeline without affecting clients that don't opt into discovery.
/// </summary>
public sealed class ServiceDiscoveryHandler(IConsulServiceResolver resolver) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (request.RequestUri is { IsAbsoluteUri: true } uri && uri.Host.EndsWith("-service", StringComparison.OrdinalIgnoreCase))
        {
            var resolved = await resolver.ResolveAsync(uri.Host, cancellationToken);
            request.RequestUri = new UriBuilder(uri)
            {
                Scheme = resolved.Scheme,
                Host = resolved.Host,
                Port = resolved.Port
            }.Uri;
        }
        return await base.SendAsync(request, cancellationToken);
    }
}

/// <summary>
/// Registers this instance in Consul on startup (with an HTTP health check pointed at /health)
/// and deregisters it on shutdown. Registration failures -- e.g. Consul isn't running during a
/// plain `dotnet run` -- are logged, not thrown, so the service still starts and serves traffic;
/// it just won't be discoverable until Consul is reachable and the service restarts. Same
/// "degrade gracefully" approach already used for the database and the Pet Service client.
/// </summary>
public sealed class ConsulRegistrationHostedService(
    IHttpClientFactory httpClientFactory,
    IOptions<ServiceRegistrationOptions> options,
    ILogger<ConsulRegistrationHostedService> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var registration = options.Value;
        var body = new
        {
            Name = registration.Name,
            ID = registration.Id,
            Address = registration.Address,
            Port = registration.Port,
            Tags = new[] { "petcare", "dotnet10", registration.Name },
            Check = new
            {
                HTTP = $"http://{registration.Address}:{registration.Port}/health",
                Interval = "10s",
                Timeout = "3s",
                DeregisterCriticalServiceAfter = "1m"
            }
        };

        try
        {
            var client = httpClientFactory.CreateClient("consul");
            using var response = await client.PutAsJsonAsync("/v1/agent/service/register", body, cancellationToken);
            response.EnsureSuccessStatusCode();
            logger.LogInformation(
                "Registered {ServiceName} ({ServiceId}) in Consul at {Address}:{Port}.",
                registration.Name, registration.Id, registration.Address, registration.Port);
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception,
                "Could not register {ServiceName} ({ServiceId}) in Consul. The service will keep " +
                "running, but other components won't be able to discover it until Consul is " +
                "reachable and this instance restarts.",
                registration.Name, registration.Id);
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        var registration = options.Value;
        try
        {
            var client = httpClientFactory.CreateClient("consul");
            using var response = await client.PutAsync(
                $"/v1/agent/service/deregister/{Uri.EscapeDataString(registration.Id)}", null, cancellationToken);
            response.EnsureSuccessStatusCode();
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Could not deregister {ServiceId} from Consul.", registration.Id);
        }
    }
}

public static class ConsulDependencyInjection
{
    /// <summary>
    /// Wires up Consul-based service discovery: config binding (Consul / ServiceRegistration
    /// sections), an HttpClient for the Consul agent API, an <see cref="IConsulServiceResolver"/>
    /// any other client can use to look up healthy instances of another service, and a hosted
    /// service that registers/deregisters this instance with Consul.
    /// </summary>
    public static IServiceCollection AddPetCareConsul(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<ConsulOptions>(configuration.GetSection("Consul"));
        services.Configure<ServiceRegistrationOptions>(configuration.GetSection("ServiceRegistration"));

        services.AddHttpClient("consul", (provider, client) =>
        {
            var options = provider.GetRequiredService<IOptions<ConsulOptions>>().Value;
            client.BaseAddress = new Uri(options.Address);
        });

        services.AddSingleton<IConsulServiceResolver, ConsulServiceResolver>();
        services.AddTransient<ServiceDiscoveryHandler>();
        services.AddHostedService<ConsulRegistrationHostedService>();

        return services;
    }
}
