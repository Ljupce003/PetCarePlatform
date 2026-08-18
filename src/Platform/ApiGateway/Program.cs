using System.Security.Claims;
using System.Text;
using ApiGateway.OpenApi;
using ApiGateway.Security;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

var jwtSection = builder.Configuration.GetRequiredSection("Jwt");
var issuer = jwtSection["Issuer"]
    ?? throw new InvalidOperationException("Jwt:Issuer is required.");
var audience = jwtSection["Audience"]
    ?? throw new InvalidOperationException("Jwt:Audience is required.");
var signingKey = jwtSection["SigningKey"];
var useLegacyDevelopmentTokens = !builder.Environment.IsEnvironment("Docker") &&
                                 !string.IsNullOrWhiteSpace(signingKey);

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;

        if (!useLegacyDevelopmentTokens)
        {
            options.Authority = (jwtSection["Authority"]
                ?? throw new InvalidOperationException("Jwt:Authority is required in Docker."))
                .TrimEnd('/');
            options.Audience = audience;
            options.RequireHttpsMetadata = jwtSection.GetValue("RequireHttpsMetadata", true);
        }

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = issuer,
            ValidateAudience = true,
            ValidAudience = audience,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = useLegacyDevelopmentTokens
                ? new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey!))
                : null,
            NameClaimType = "preferred_username",
            RoleClaimType = ClaimTypes.Role,
            ClockSkew = TimeSpan.FromSeconds(30)
        };
    });
builder.Services.AddTransient<IClaimsTransformation, KeycloakRoleClaimsTransformation>();
builder.Services.AddAuthorization();

var frontendOrigins = builder.Configuration.GetSection("Frontend:AllowedOrigins").Get<string[]>()
    ?? ["http://localhost:5173"];
builder.Services.AddCors(options => options.AddPolicy("petcare-frontend", policy => policy
    .WithOrigins(frontendOrigins)
    .AllowAnyHeader()
    .AllowAnyMethod()));

builder.Services.AddReverseProxy().LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));
builder.Services.AddHttpClient("gateway-openapi", client =>
    client.Timeout = TimeSpan.FromSeconds(10));
builder.Services.AddSingleton<GatewayOpenApiDocumentProvider>();

builder.Services.AddHealthChecks();


var app = builder.Build();

app.UseSwaggerUI(options =>
{
    options.RoutePrefix = "swagger";
    options.DocumentTitle = "PetCare API Gateway Documentation";
    options.SwaggerEndpoint("/openapi/pet.json", "Pet Service");
    options.SwaggerEndpoint("/openapi/appointment.json", "Appointment Service");
    options.SwaggerEndpoint("/openapi/treatment.json", "Treatment & Notification Service");
    options.DisplayRequestDuration();
    options.EnablePersistAuthorization();
});

app.MapGet("/", () => Results.Redirect("/swagger"));
app.MapGet("/docs", () => Results.Redirect("/swagger"));

app.MapGet("/openapi", () => Results.Ok(new
{
    title = "PetCare API Gateway",
    swaggerUi = "/swagger",
    services = GatewayOpenApiCatalog.Services.Values.Select(service => new
    {
        service.Id,
        service.DisplayName,
        gatewayPrefix = service.GatewayPrefix,
        openApiDocument = $"/openapi/{service.Id}.json"
    }),
    mcp = new
    {
        endpoint = "/mcp",
        documentation = "README.md at the repository root",
        note = "MCP uses JSON-RPC over Streamable HTTP and is not described by OpenAPI."
    }
}));

app.MapGet("/openapi/{serviceId}.json", async (
    string serviceId,
    GatewayOpenApiDocumentProvider documents,
    ILogger<Program> logger,
    CancellationToken cancellationToken) =>
{
    if (!GatewayOpenApiCatalog.Services.TryGetValue(serviceId, out var service))
        return Results.NotFound(new { error = $"Unknown service '{serviceId}'." });

    try
    {
        return Results.Json(await documents.GetAsync(service, cancellationToken));
    }
    catch (Exception exception) when (exception is HttpRequestException or
                                      TaskCanceledException or
                                      InvalidOperationException)
    {
        logger.LogError(exception,
            "Could not load the OpenAPI document for {ServiceName}.", service.DisplayName);
        return Results.Problem(
            statusCode: StatusCodes.Status502BadGateway,
            title: $"{service.DisplayName} documentation is unavailable",
            detail: "The Gateway could not retrieve a valid OpenAPI document from the downstream service.");
    }
});

app.MapHealthChecks("/health");

app.UseCors("petcare-frontend");
app.UseAuthentication();
app.UseAuthorization();

app.MapReverseProxy();

app.Run();

public partial class Program;
