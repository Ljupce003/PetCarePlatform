using Microsoft.EntityFrameworkCore;

namespace AppointmentService.Infrastructure.Persistence;

/// <summary>
/// Ensures the Appointment Service database exists on startup. Once EF Core migrations
/// are introduced alongside the domain model, this should call
/// <c>dbContext.Database.MigrateAsync()</c> instead of <c>EnsureCreatedAsync()</c>.
/// </summary>
public static class AppointmentDbInitializer
{
    public static Task InitializeAsync(AppointmentDbContext dbContext, CancellationToken cancellationToken = default)
        => dbContext.Database.EnsureCreatedAsync(cancellationToken);
}
