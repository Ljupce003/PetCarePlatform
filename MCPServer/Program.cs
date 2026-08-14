using System.Text;
using MCPServer.Clients;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using ModelContextProtocol.Protocol;

var builder = WebApplication.CreateBuilder(args);

var jwtSection = builder.Configuration.GetSection("Jwt");
var jwtIssuer = jwtSection["Issuer"]
    ?? throw new InvalidOperationException("Jwt:Issuer is required.");
var jwtAudience = jwtSection["Audience"]
    ?? throw new InvalidOperationException("Jwt:Audience is required.");
var jwtSigningKey = jwtSection["SigningKey"]
    ?? throw new InvalidOperationException(
        "Jwt:SigningKey is required. Set it through configuration or Jwt__SigningKey.");

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtIssuer,
            ValidateAudience = true,
            ValidAudience = jwtAudience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSigningKey)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30)
        };
    });
builder.Services.AddAuthorization();

builder.Services.AddHttpContextAccessor();
builder.Services.AddTransient<BearerTokenForwardingHandler>();

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
    .AddHttpMessageHandler<BearerTokenForwardingHandler>();

builder.Services
    .AddHttpClient<PetServiceClient>((services, client) =>
    {
        var baseUrl = services.GetRequiredService<IConfiguration>()["Services:PetServiceUrl"]
            ?? throw new InvalidOperationException("Services:PetServiceUrl is required.");
        client.BaseAddress = new Uri(baseUrl);
        client.Timeout = TimeSpan.FromSeconds(30);
    })
    .AddHttpMessageHandler<BearerTokenForwardingHandler>();

builder.Services
    .AddHttpClient<AppointmentServiceClient>((services, client) =>
    {
        var baseUrl = services.GetRequiredService<IConfiguration>()["Services:AppointmentServiceUrl"]
            ?? throw new InvalidOperationException("Services:AppointmentServiceUrl is required.");
        client.BaseAddress = new Uri(baseUrl);
        client.Timeout = TimeSpan.FromSeconds(30);
    })
    .AddHttpMessageHandler<BearerTokenForwardingHandler>();

builder.Services
    .AddMcpServer(options =>
    {
        options.ServerInfo = new Implementation
        {
            Name = "PetCare MCP Server",
            Title = "PetCare Platform MCP Server",
            Version = "1.0.0",
            Description = "Secure MCP tools for the PetCare microservices."
        };
    })
    .WithHttpTransport(options => options.Stateless = true)
    .WithToolsFromAssembly();


builder.Services.AddHealthChecks();

var app = builder.Build();

app.MapHealthChecks("/health");

app.UseAuthentication();
app.UseAuthorization();

app.MapMcp("/mcp").RequireAuthorization();

app.Run();

// WebApplicationFactory-based MCP integration tests need access to the top-level Program type.
public partial class Program;
