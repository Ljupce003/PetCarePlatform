using MCPServer.Clients;
using ModelContextProtocol.Protocol;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpClient("keycloak-service-token", (services, client) =>
{
    var authority = services.GetRequiredService<IConfiguration>()["ServiceAuthentication:Authority"]
        ?? throw new InvalidOperationException("ServiceAuthentication:Authority is required.");
    client.BaseAddress = new Uri(authority.TrimEnd('/') + "/");
    client.Timeout = TimeSpan.FromSeconds(10);
});
builder.Services.AddSingleton<IServiceAccessTokenProvider, KeycloakServiceAccessTokenProvider>();
builder.Services.AddTransient<ServiceAccessTokenHandler>();

builder.Services
    .AddHttpClient<TreatmentServiceClient>((services, client) =>
    {
        var configuration = services
            .GetRequiredService<IConfiguration>();

        var baseUrl = configuration[
                          "Services:TreatmentServiceUrl"]
                      ?? throw new InvalidOperationException(
                          "Services:TreatmentServiceUrl is required.");

        client.BaseAddress = new Uri(baseUrl);
        client.Timeout = TimeSpan.FromSeconds(30);
    })
    .AddHttpMessageHandler<ServiceAccessTokenHandler>();

builder.Services
    .AddHttpClient<PetServiceClient>((services, client) =>
    {
        var baseUrl = services.GetRequiredService<IConfiguration>()["Services:PetServiceUrl"]
            ?? throw new InvalidOperationException("Services:PetServiceUrl is required.");
        client.BaseAddress = new Uri(baseUrl);
        client.Timeout = TimeSpan.FromSeconds(30);
    })
    .AddHttpMessageHandler<ServiceAccessTokenHandler>();

builder.Services
    .AddHttpClient<AppointmentServiceClient>((services, client) =>
    {
        var baseUrl = services.GetRequiredService<IConfiguration>()["Services:AppointmentServiceUrl"]
            ?? throw new InvalidOperationException("Services:AppointmentServiceUrl is required.");
        client.BaseAddress = new Uri(baseUrl);
        client.Timeout = TimeSpan.FromSeconds(30);
    })
    .AddHttpMessageHandler<ServiceAccessTokenHandler>();

builder.Services
    .AddMcpServer(options =>
    {
        options.ServerInfo = new Implementation
        {
            Name = "PetCare MCP Server",
            Title = "PetCare Platform MCP Server",
            Version = "1.0.0",
            Description = "Trusted administrative MCP tools for the PetCare microservices."
        };
    })
    .WithHttpTransport(options => options.Stateless = true)
    .WithToolsFromAssembly();


builder.Services.AddHealthChecks();

var app = builder.Build();

app.MapHealthChecks("/health");

// This is deliberately a trusted, anonymous MCP endpoint. Each downstream call is made with
// the MCP service account; callers select the affected owner/veterinarian through tool arguments.
app.MapMcp("/mcp");

app.Run();

// WebApplicationFactory-based MCP integration tests need access to the top-level Program type.
public partial class Program;
