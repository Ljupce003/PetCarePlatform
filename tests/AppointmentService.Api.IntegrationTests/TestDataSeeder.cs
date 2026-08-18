using AppointmentService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace AppointmentService.Api.IntegrationTests;

/// <summary>
/// Creates the InMemory database's schema and seeds AppointmentDbInitializer's demo data on host
/// startup -- the InMemory-provider-compatible equivalent of what Program.cs already does with
/// Database.MigrateAsync() for a real Postgres database.
/// </summary>
internal sealed class TestDataSeeder(IServiceScopeFactory scopeFactory) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppointmentDbContext>();
        await dbContext.Database.EnsureCreatedAsync(cancellationToken);
        await AppointmentDbInitializer.SeedIfEmptyAsync(dbContext, cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
