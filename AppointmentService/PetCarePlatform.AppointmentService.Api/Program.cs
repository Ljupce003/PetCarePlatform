using Microsoft.OpenApi;
using PetCarePlatform.AppointmentService.Infrastructure;
using PetCarePlatform.AppointmentService.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "PetCarePlatform Appointment Service",
        Version = "v1",
        Description = "Appointment bounded context: clinics, veterinarians, availability slots and appointments."
    });
});
builder.Services.AddAppointmentServiceInfrastructure(builder.Configuration);
builder.Services.AddHealthChecks()
    .AddDbContextCheck<AppointmentDbContext>("appointment-database");

var app = builder.Build();

// Configure the HTTP request pipeline.
// Swagger is enabled in every environment (not just Development) so it is easy to check
// that the service is up, both locally and when running via Docker.
app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "Appointment Service v1");
    options.RoutePrefix = "swagger";
});

app.UseHttpsRedirection();
app.MapControllers();
app.MapHealthChecks("/health");

// Make sure the Appointment Service database exists. Failures are logged, not thrown,
// so the service still starts and reports itself via /health even if PostgreSQL is not
// reachable yet (e.g. first `docker compose up`, or running the API before its database).
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

app.Run();

// Exposed for WebApplicationFactory-based integration tests.
public partial class Program;
