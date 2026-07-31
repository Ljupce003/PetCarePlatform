using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using AppointmentService.Application;
using AppointmentService.Infrastructure.Persistence;

namespace AppointmentService.Infrastructure;

public static class DependencyInjection
{
    /// <summary>
    /// Registers persistence and, through it, the application layer. This is the single
    /// entry point the API composes, which keeps the outermost layer unaware of both
    /// EF Core and the internals of the application layer.
    /// </summary>
    public static IServiceCollection AddAppointmentServiceInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Database")
            ?? throw new InvalidOperationException(
                "Connection string 'Database' is not configured. Set ConnectionStrings:Database in appsettings " +
                "or the ConnectionStrings__Database environment variable.");

        services.AddDbContext<AppointmentDbContext>(options => options.UseNpgsql(connectionString));

        services.AddAppointmentServiceApplication();
        return services;
    }
}
