using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;
using AppointmentService.Api.OpenApi;
using AppointmentService.Application.Exceptions;
using AppointmentService.Domain.Exceptions;
using AppointmentService.Infrastructure;
using AppointmentService.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

// Structured logging: plain text is easier to read while developing, JSON everywhere else
// so a log collector can index the fields instead of parsing strings.
builder.Logging.ClearProviders();
if (builder.Environment.IsDevelopment())
{
    builder.Logging.AddSimpleConsole(options =>
    {
        options.SingleLine = true;
        options.TimestampFormat = "HH:mm:ss ";
    });
}
else
{
    builder.Logging.AddJsonConsole(options =>
    {
        options.IncludeScopes = true;
        options.JsonWriterOptions = new JsonWriterOptions { Indented = false };
    });
}

builder.Services.AddControllers();

// Validates the JWTs this same service issues from /auth/login and /auth/token -- a stand-in for
// validating tokens from Keycloak until that exists (see README's "Security and authorization").
var jwtSection = builder.Configuration.GetSection("Jwt");
var jwtSigningKey = jwtSection["SigningKey"]
    ?? throw new InvalidOperationException(
        "Jwt:SigningKey is not configured. Set Jwt:SigningKey in appsettings or the Jwt__SigningKey environment variable.");

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtSection["Issuer"],
            ValidateAudience = true,
            ValidAudience = jwtSection["Audience"],
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSigningKey)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30)
        };
    });
builder.Services.AddAuthorization();

builder.Services.AddOpenApi(options =>
{
    // Lets Swagger UI show an "Authorize" button and attach the Bearer token to requests.
    options.AddDocumentTransformer<BearerSecuritySchemeTransformer>();
    options.AddOperationTransformer<AuthorizeOperationTransformer>();
});

builder.Services.AddHealthChecks()
    .AddDbContextCheck<AppointmentDbContext>("appointment-database", tags: ["ready"]);

builder.Services.AddAppointmentServiceInfrastructure(builder.Configuration);

var app = builder.Build();

// Maps the exceptions thrown by the Application/Domain layers to the HTTP status a REST client
// actually expects, instead of letting everything fall through as a bare 500.
app.UseExceptionHandler(errorApp => errorApp.Run(HandleExceptionAsync));

app.MapOpenApi();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/openapi/v1.json", "Appointment Service v1");
    options.RoutePrefix = "swagger";
});

// /health covers the service and its dependencies; /health/live only asks whether the
// process is up, which is what a container orchestrator should restart on. Neither carries
// [Authorize] metadata, so both stay reachable without a token (Consul's health check depends on it).
app.MapHealthChecks("/health", new HealthCheckOptions { ResponseWriter = WriteHealthResponseAsync });
app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = _ => false,
    ResponseWriter = WriteHealthResponseAsync
});

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Make sure the Appointment Service database exists before accepting traffic. Failures are
// logged, not thrown, so the service still starts and reports itself via /health even if
// PostgreSQL is not reachable yet (e.g. first `docker compose up`).
await using (var scope = app.Services.CreateAsyncScope())
{
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    try
    {
        var dbContext = scope.ServiceProvider.GetRequiredService<AppointmentDbContext>();
        await AppointmentDbInitializer.InitializeAsync(dbContext);
        logger.LogInformation("Appointment Service database is ready");
    }
    catch (Exception ex)
    {
        logger.LogWarning(ex, "Could not reach the Appointment Service database on startup; it will be reported as unhealthy until it becomes reachable");
    }
}

await app.RunAsync();

// Translates domain/application exceptions into the right HTTP status + a small JSON body,
// instead of the framework's default bare 500 for anything that isn't a plain-old bad request.
static async Task HandleExceptionAsync(HttpContext context)
{
    var exception = context.Features.Get<IExceptionHandlerFeature>()?.Error;

    var (statusCode, title) = exception switch
    {
        ValidationException => (StatusCodes.Status400BadRequest, "Validation failed"),
        KeyNotFoundException => (StatusCodes.Status404NotFound, "Not found"),
        PetOwnershipException => (StatusCodes.Status403Forbidden, "Pet is not owned by this owner"),
        SlotAlreadyBookedException or SlotExpiredException or InvalidAppointmentStatusTransitionException
            => (StatusCodes.Status409Conflict, "Conflict"),
        _ => (StatusCodes.Status500InternalServerError, "Unexpected error")
    };

    var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
    if (statusCode == StatusCodes.Status500InternalServerError)
    {
        logger.LogError(exception, "Unhandled exception while processing {Path}", context.Request.Path);
    }
    else
    {
        logger.LogInformation(exception, "{Title} while processing {Path}", title, context.Request.Path);
    }

    context.Response.StatusCode = statusCode;
    context.Response.ContentType = "application/problem+json";
    await context.Response.WriteAsJsonAsync(new
    {
        title,
        status = statusCode,
        detail = exception?.Message,
        errors = (exception as ValidationException)?.Errors
    });
}

// Reports which check failed instead of the default one-word body.
static Task WriteHealthResponseAsync(HttpContext context, HealthReport report)
{
    context.Response.ContentType = "application/json";
    return context.Response.WriteAsJsonAsync(new
    {
        status = report.Status.ToString(),
        totalDurationMs = report.TotalDuration.TotalMilliseconds,
        checks = report.Entries.Select(entry => new
        {
            name = entry.Key,
            status = entry.Value.Status.ToString(),
            description = entry.Value.Description ?? entry.Value.Exception?.Message
        })
    });
}

// Top-level statements make Program an internal, compiler-generated class. WebApplicationFactory
// <Program> (used by AppointmentService.Api.IntegrationTests) needs it visible from outside this
// assembly, hence this explicit, otherwise-unused partial declaration.
public partial class Program;
