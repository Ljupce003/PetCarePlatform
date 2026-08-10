using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Converters;
using TreatmentAndNotificationService.API.ExceptionHandling;
using TreatmentAndNotificationService.Application;
using TreatmentAndNotificationService.Application.Services;
using TreatmentAndNotificationService.Application.Services.Impl;
using TreatmentAndNotificationService.Domain.Repositories;
using TreatmentAndNotificationService.Infrastructure.Messaging;
using TreatmentAndNotificationService.Infrastructure.Notifications;
using TreatmentAndNotificationService.Infrastructure.Persistence;
using TreatmentAndNotificationService.Infrastructure.Persistence.RepoImpl;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddControllers().AddNewtonsoftJson(options =>
    options.SerializerSettings.Converters.Add(new StringEnumConverter()));
builder.Services.AddHealthChecks();

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
app.UseSwaggerUI();
app.UseHttpsRedirection();
app.MapHealthChecks("/health");
app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<TreatmentDbContext>();
    await context.Database.MigrateAsync();
    if (app.Environment.IsDevelopment())
        await TreatmentDbContextSeeder.SeedAsync(context);
}

await app.RunAsync();

// Makes the top-level Program type accessible to WebApplicationFactory integration tests.
public partial class Program;
