using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using TreatmentAndNotificationService.Infrastructure.Messaging;
using TreatmentAndNotificationService.Infrastructure.Persistence;

namespace TreatmentAndNotificationService.Api.IntegrationTests;

/// <summary>
/// Boots the production Kafka consumer against disposable Kafka and PostgreSQL containers while
/// removing the delivery worker so tests can assert the notifications in their pending state.
/// </summary>
public sealed class TreatmentKafkaFactory(
    string connectionString,
    string bootstrapServers,
    string topic,
    string deadLetterTopic) : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("Kafka:BootstrapServers", bootstrapServers);
        builder.UseSetting("Kafka:GroupId", $"treatment-kafka-tests-{Guid.NewGuid():N}");
        builder.UseSetting("Kafka:Topic", topic);
        builder.UseSetting("Kafka:DeadLetterTopic", deadLetterTopic);
        builder.UseSetting("Kafka:MaxProcessingAttempts", "2");
        builder.UseSetting("Kafka:RetryDelayMilliseconds", "50");

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<TreatmentDbContext>>();
            services.AddDbContext<TreatmentDbContext>(options => options.UseNpgsql(connectionString));

            // Program registers both background services. Only the Kafka consumer is needed here;
            // allowing the delivery worker to run would change Pending notifications to Sent.
            services.RemoveAll<IHostedService>();
            services.AddHostedService<AppointmentEventConsumer>();
        });
    }

    public async Task ResetDatabaseAsync()
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TreatmentDbContext>();
        await db.Database.ExecuteSqlRawAsync(
            "TRUNCATE TABLE notifications, medical_examinations, vaccinations;");
    }

    public async Task<TResult> WithDbContextAsync<TResult>(Func<TreatmentDbContext, Task<TResult>> action)
    {
        using var scope = Services.CreateScope();
        return await action(scope.ServiceProvider.GetRequiredService<TreatmentDbContext>());
    }
}
