using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Converters;
using TreatmentAndNotificationService.Repository;
using TreatmentAndNotificationService.Repository.Impl;
using TreatmentAndNotificationService.Service;
using TreatmentAndNotificationService.Service.Impl;

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

builder.Services.AddDbContext<TreatmentNotificationDbContext>(options =>
{
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"));
});

builder.Services.AddTransient<ITreatmentRepository, TreatmentRepository>();
builder.Services.AddTransient<ITreatmentService, TreatmentService>();


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
