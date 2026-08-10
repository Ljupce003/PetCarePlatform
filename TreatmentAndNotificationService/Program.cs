using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json.Converters;
using TreatmentAndNotificationService.API.ExceptionHandling;
using TreatmentAndNotificationService.API.OpenApi;
using TreatmentAndNotificationService.Application;
using TreatmentAndNotificationService.Application.Services;
using TreatmentAndNotificationService.Application.Services.Impl;
using TreatmentAndNotificationService.Domain.Repositories;
using TreatmentAndNotificationService.Infrastructure.Messaging;
using TreatmentAndNotificationService.Infrastructure.Notifications;
using TreatmentAndNotificationService.Infrastructure.Persistence;
using TreatmentAndNotificationService.Infrastructure.Persistence.RepoImpl;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer<BearerSecuritySchemeTransformer>();
    options.AddOperationTransformer<AuthorizeOperationTransformer>();
});
builder.Services.AddControllers().AddNewtonsoftJson(options =>
    options.SerializerSettings.Converters.Add(new StringEnumConverter()));
builder.Services.AddHealthChecks();

// Appointment Service temporarily acts as the local token issuer until the shared Keycloak realm
// exists. Treatment Service validates the same issuer, audience, signing key, lifetime, and role
// claims, so both human and client-credentials tokens work consistently across service boundaries.
var jwtSection = builder.Configuration.GetSection("Jwt");
var jwtSigningKey = jwtSection["SigningKey"]
    ?? throw new InvalidOperationException(
        "Jwt:SigningKey is not configured. Set Jwt:SigningKey or Jwt__SigningKey.");

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

var connectionString = builder.Configuration.GetConnectionString("Database")
    ?? builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("A treatment database connection string is required.");
builder.Services.AddDbContext<TreatmentDbContext>(options => options.UseNpgsql(connectionString));

builder.Services.AddScoped<IMedicalExaminationRepository, MedicalExaminationRepository>();
builder.Services.AddScoped<IVaccinationRepository, VaccinationRepository>();
builder.Services.AddScoped<INotificationRepository, NotificationRepository>();
builder.Services.AddScoped<IUnitOfWork>(serviceProvider => serviceProvider.GetRequiredService<TreatmentDbContext>());
builder.Services.AddScoped<IAppointmentNotificationApplicationService, AppointmentNotificationApplicationService>();
builder.Services.AddTreatmentApplication();
builder.Services.AddSingleton<INotificationSender, ConsoleNotificationSender>();
builder.Services.AddHostedService<NotificationDeliveryWorker>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    var xmlFile = $"{typeof(Program).Assembly.GetName().Name}.xml";
    options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, xmlFile));
});
builder.Services.AddSwaggerGenNewtonsoftSupport();


builder.Services.AddOptions<KafkaConsumerOptions>()
    .Bind(builder.Configuration.GetSection(KafkaConsumerOptions.SectionName))
    .Validate(options => !string.IsNullOrWhiteSpace(options.BootstrapServers),
        "Kafka:BootstrapServers is required.")
    .Validate(options => !string.IsNullOrWhiteSpace(options.GroupId),
        "Kafka:GroupId is required.")
    .Validate(options => !string.IsNullOrWhiteSpace(options.Topic),
        "Kafka:Topic is required.")
    .Validate(options => !string.IsNullOrWhiteSpace(options.DeadLetterTopic),
        "Kafka:DeadLetterTopic is required.")
    .Validate(options => options.MaxProcessingAttempts > 0,
        "Kafka:MaxProcessingAttempts must be greater than zero.")
    .Validate(options => options.RetryDelayMilliseconds >= 0,
        "Kafka:RetryDelayMilliseconds cannot be negative.")
    .ValidateOnStart();
builder.Services.AddHostedService<AppointmentEventConsumer>();


var app = builder.Build();
app.UseMiddleware<DomainExceptionMiddleware>();
app.MapOpenApi();
app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/openapi/v1.json", "Treatment & Notification Service v1");
});
app.UseHttpsRedirection();
app.MapHealthChecks("/health");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<TreatmentDbContext>();
    await context.Database.MigrateAsync();
    if (app.Environment.IsDevelopment())
        await TreatmentDbContextSeeder.SeedAsync(context);
}

await app.RunAsync();
