using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using TreatmentAndNotificationService.Infrastructure.Persistence;

namespace TreatmentAndNotificationService.Api.IntegrationTests;

/// <summary>
/// Boots the production API composition against Testcontainers PostgreSQL. The worker is removed
/// to keep notification state deterministic; all controllers, handlers, repositories, mappings,
/// middleware, migrations, and Npgsql queries are otherwise real production components.
/// </summary>
public sealed class TreatmentApiFactory(string connectionString) : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<TreatmentDbContext>>();
            services.AddDbContext<TreatmentDbContext>(options => options.UseNpgsql(connectionString));
            services.RemoveAll<IHostedService>();
        });
    }

    public async Task ResetDatabaseAsync()
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TreatmentDbContext>();
        await db.Database.ExecuteSqlRawAsync("TRUNCATE TABLE notifications, medical_examinations, vaccinations;");
    }

    public async Task<TResult> WithDbContextAsync<TResult>(Func<TreatmentDbContext, Task<TResult>> action)
    {
        using var scope = Services.CreateScope();
        return await action(scope.ServiceProvider.GetRequiredService<TreatmentDbContext>());
    }
}
