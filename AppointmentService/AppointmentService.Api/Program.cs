using System.Text.Json;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
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

builder.Services.AddOpenApi();

builder.Services.AddHealthChecks()
    .AddDbContextCheck<AppointmentDbContext>("appointment-database", tags: ["ready"]);

builder.Services.AddAppointmentServiceInfrastructure(builder.Configuration);

var app = builder.Build();

app.MapOpenApi();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/openapi/v1.json", "Appointment Service v1");
    options.RoutePrefix = "swagger";
});

// /health covers the service and its dependencies; /health/live only asks whether the
// process is up, which is what a container orchestrator should restart on.
app.MapHealthChecks("/health", new HealthCheckOptions { ResponseWriter = WriteHealthResponseAsync });
app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = _ => false,
    ResponseWriter = WriteHealthResponseAsync
});

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
