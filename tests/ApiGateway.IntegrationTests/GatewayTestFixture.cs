using System.Collections.Concurrent;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace ApiGateway.IntegrationTests;

public sealed class GatewayTestFixture : IAsyncLifetime
{
    public const string Issuer = "appointment-service";
    public const string Audience = "petcare";
    public const string SigningKey = "dev-only-signing-key-change-me-32-chars-minimum!!";

    public DownstreamTestServer Pet { get; private set; } = null!;
    public DownstreamTestServer Appointment { get; private set; } = null!;
    public DownstreamTestServer Treatment { get; private set; } = null!;
    public DownstreamTestServer Mcp { get; private set; } = null!;
    public GatewayFactory Gateway { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        Pet = await DownstreamTestServer.StartAsync("pet");
        Appointment = await DownstreamTestServer.StartAsync("appointment");
        Treatment = await DownstreamTestServer.StartAsync("treatment");
        Mcp = await DownstreamTestServer.StartAsync("mcp");

        Gateway = new GatewayFactory(new GatewayDestinations(
            Pet.Address,
            Appointment.Address,
            Treatment.Address,
            Mcp.Address));

        // Force WebApplicationFactory to start now so configuration errors fail fixture setup.
        using var client = Gateway.CreateClient();
        using var response = await client.GetAsync("/health");
        response.EnsureSuccessStatusCode();
    }

    public async Task DisposeAsync()
    {
        if (Gateway is not null)
            await Gateway.DisposeAsync();
        if (Mcp is not null)
            await Mcp.DisposeAsync();
        if (Treatment is not null)
            await Treatment.DisposeAsync();
        if (Appointment is not null)
            await Appointment.DisposeAsync();
        if (Pet is not null)
            await Pet.DisposeAsync();
    }

    public HttpClient CreateAuthenticatedClient(string role = "owner")
    {
        var client = Gateway.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", CreateToken(role: role));
        return client;
    }

    public static string CreateToken(
        string issuer = Issuer,
        string audience = Audience,
        string signingKey = SigningKey,
        DateTime? expires = null,
        string? role = "owner")
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, "gateway-integration-test"),
            new("preferred_username", "gateway-test-user")
        };
        if (role is not null)
            claims.Add(new Claim(ClaimTypes.Role, role));

        var expiry = expires ?? DateTime.UtcNow.AddMinutes(10);
        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            notBefore: expiry.AddMinutes(-20),
            expires: expiry,
            signingCredentials: new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)),
                SecurityAlgorithms.HmacSha256));

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}

public sealed record GatewayDestinations(
    string Pet,
    string Appointment,
    string Treatment,
    string Mcp);

public sealed class GatewayFactory(GatewayDestinations destinations) : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AllowedHosts"] = "*",
                ["Jwt:Issuer"] = GatewayTestFixture.Issuer,
                ["Jwt:Audience"] = GatewayTestFixture.Audience,
                ["Jwt:SigningKey"] = GatewayTestFixture.SigningKey,
                ["ReverseProxy:Clusters:pet-cluster:Destinations:pet-service:Address"] = destinations.Pet,
                ["ReverseProxy:Clusters:appointment-cluster:Destinations:appointment-service:Address"] = destinations.Appointment,
                ["ReverseProxy:Clusters:treatment-cluster:Destinations:treatment-service:Address"] = destinations.Treatment,
                ["ReverseProxy:Clusters:mcp-cluster:Destinations:mcp-server:Address"] = destinations.Mcp
            });
        });
    }
}

public sealed class DownstreamTestServer : IAsyncDisposable
{
    private readonly WebApplication _application;

    private DownstreamTestServer(WebApplication application, string name, string address)
    {
        _application = application;
        Name = name;
        Address = address.EndsWith('/') ? address : $"{address}/";
    }

    public string Name { get; }
    public string Address { get; }
    public ConcurrentQueue<CapturedRequest> Requests { get; } = new();

    public static async Task<DownstreamTestServer> StartAsync(string name)
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            EnvironmentName = "Testing",
            ApplicationName = typeof(DownstreamTestServer).Assembly.FullName
        });
        builder.WebHost.UseKestrel().UseUrls("http://127.0.0.1:0");

        var application = builder.Build();
        DownstreamTestServer? server = null;

        application.Run(async context =>
        {
            using var reader = new StreamReader(context.Request.Body, Encoding.UTF8);
            var body = await reader.ReadToEndAsync(context.RequestAborted);
            var request = new CapturedRequest(
                context.Request.Method,
                context.Request.Path.Value ?? string.Empty,
                context.Request.QueryString.Value ?? string.Empty,
                body,
                context.Request.Headers.ToDictionary(
                    header => header.Key,
                    header => header.Value.ToString(),
                    StringComparer.OrdinalIgnoreCase));
            server!.Requests.Enqueue(request);

            if (context.Request.Path == "/status/teapot")
            {
                context.Response.StatusCode = StatusCodes.Status418ImATeapot;
                context.Response.ContentType = "application/problem+json";
                context.Response.Headers["X-Downstream-Error"] = name;
                await context.Response.WriteAsync(
                    $$"""{"service":"{{name}}","error":"teapot"}""",
                    context.RequestAborted);
                return;
            }

            if (name == "mcp" && context.Request.Path == "/mcp")
            {
                context.Response.StatusCode = StatusCodes.Status200OK;
                context.Response.ContentType = "text/event-stream";
                context.Response.Headers.CacheControl = "no-cache,no-store";
                context.Response.Headers["X-Accel-Buffering"] = "no";
                await context.Response.WriteAsync(
                    "event: message\ndata: {\"jsonrpc\":\"2.0\",\"id\":1,\"result\":{\"tools\":[]}}\n\n",
                    context.RequestAborted);
                await context.Response.Body.FlushAsync(context.RequestAborted);
                return;
            }

            context.Response.StatusCode = StatusCodes.Status200OK;
            await context.Response.WriteAsJsonAsync(new DownstreamEcho(
                name,
                request.Method,
                request.Path,
                request.Query,
                request.Body,
                request.Header("Authorization"),
                request.Header("X-Correlation-ID"),
                request.Header("MCP-Protocol-Version")), context.RequestAborted);
        });

        await application.StartAsync();
        var addresses = application.Services
            .GetRequiredService<IServer>()
            .Features
            .Get<IServerAddressesFeature>()
            ?.Addresses;
        var address = addresses?.Single()
            ?? throw new InvalidOperationException($"Could not resolve the {name} test-server address.");

        server = new DownstreamTestServer(application, name, address);
        return server;
    }

    public async ValueTask DisposeAsync()
    {
        await _application.StopAsync();
        await _application.DisposeAsync();
    }
}

public sealed record CapturedRequest(
    string Method,
    string Path,
    string Query,
    string Body,
    IReadOnlyDictionary<string, string> Headers)
{
    public string? Header(string name) => Headers.GetValueOrDefault(name);
}

public sealed record DownstreamEcho(
    string Service,
    string Method,
    string Path,
    string Query,
    string Body,
    string? Authorization,
    string? CorrelationId,
    string? McpProtocolVersion);
