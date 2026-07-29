using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PetCarePlatform.AppointmentService.Infrastructure.Persistence;

namespace PetCarePlatform.AppointmentService.Infrastructure;

/// <summary>
/// Wires up everything the Infrastructure layer owns (EF Core/PostgreSQL today;
/// repositories, outbound HTTP clients and messaging as they are added later) behind a
/// single extension method the API layer calls from Program.cs.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddAppointmentServiceInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Database")
            ?? throw new InvalidOperationException("Connection string 'Database' is not configured.");

        services.AddDbContext<AppointmentDbContext>(options => options.UseNpgsql(connectionString));

        // Repository interfaces/implementations are registered here as they are added
        // together with the Application layer use cases.

        return services;
    }
}
