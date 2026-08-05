using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Converters;
using TreatmentAndNotificationService.Application.Services;
using TreatmentAndNotificationService.Application.Services.Impl;
using TreatmentAndNotificationService.Infrastructure.Persistence;
using TreatmentAndNotificationService.Infrastructure.Persistence.RepoImpl;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddControllers()
    .AddNewtonsoftJson(options =>
    {
        options.SerializerSettings.Converters.Add(new StringEnumConverter());
    });

builder.Services.AddHealthChecks();

builder.Services.AddDbContext<TreatmentDbContext>(options =>
{
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"));
});

//Repos
builder.Services.AddScoped<IMedicalExaminationRepository, MedicalExaminationRepository>();
builder.Services.AddScoped<IVaccinationRepository, VaccinationRepository>();
builder.Services.AddScoped<INotificationRepository, NotificationRepository>();

//Services
builder.Services.AddScoped<ITreatmentApplicationService, TreatmentApplicationService>();
builder.Services.AddScoped<IAppointmentNotificationApplicationService, AppointmentNotificationApplicationService>();


builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{

    var xmlFile = $"{typeof(Program).Assembly.GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    c.IncludeXmlComments(xmlPath);
});
builder.Services.AddSwaggerGenNewtonsoftSupport();

var app = builder.Build();

// // Configure the HTTP request pipeline.
// if (app.Environment.IsDevelopment())
// {
//     app.MapOpenApi();
// }


app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();
app.UseRouting();
// app.UseExceptionHandler();
// app.UseAuthentication();
// app.UseAuthorization();
// app.MapOpenApi();
app.MapHealthChecks("/health");
app.MapControllers();


app.Run();
