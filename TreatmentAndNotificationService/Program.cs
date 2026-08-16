using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json.Converters;
using Shared.ServiceDiscovery;
using TreatmentAndNotificationService.API.ExceptionHandling;
using TreatmentAndNotificationService.API.OpenApi;
using TreatmentAndNotificationService.API.Security;
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
builder.Services.AddPetCareConsul(builder.Configuration);

var jwtSection = builder.Configuration.GetRequiredSection("Jwt");
var jwtIssuer = jwtSection["Issuer"]
    ?? throw new InvalidOperationException("Jwt:Issuer is required.");
var jwtAudience = jwtSection["Audience"]
    ?? throw new InvalidOperationException("Jwt:Audience is required.");
var jwtSigningKey = jwtSection["SigningKey"];
var useLegacyDevelopmentTokens = !builder.Environment.IsEnvironment("Docker") &&
                                 !string.IsNullOrWhiteSpace(jwtSigningKey);

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
            options.Audience = jwtAudience;
            options.RequireHttpsMetadata = jwtSection.GetValue("RequireHttpsMetadata", true);
        }

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtIssuer,
            ValidateAudience = true,
            ValidAudience = jwtAudience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = useLegacyDevelopmentTokens
                ? new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSigningKey!))
                : null,
            ValidateLifetime = true,
            NameClaimType = "preferred_username",
            RoleClaimType = ClaimTypes.Role,
            ClockSkew = TimeSpan.FromSeconds(30)
        };
    });
builder.Services.AddTransient<IClaimsTransformation, KeycloakRoleClaimsTransformation>();
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
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<INotificationSender, ConsoleNotificationSender>();
builder.Services.AddScoped<NotificationDeliveryProcessor>();
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
