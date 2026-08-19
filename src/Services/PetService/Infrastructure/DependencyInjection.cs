using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PetService.Application;
using PetService.Application.Abstractions;
using PetService.Infrastructure.Persistence;
using Shared.ServiceDiscovery;

namespace PetService.Infrastructure;

public static class DependencyInjection
{
    /// <summary>
    /// Registers persistence and, through it, the application layer. This is the single
    /// entry point the API composes, which keeps the outermost layer unaware of both
    /// EF Core and the internals of the application layer.
    /// </summary>
    public static IServiceCollection AddPetServiceInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Database")
            ?? throw new InvalidOperationException(
                "Connection string 'Database' is not configured. Set ConnectionStrings:Database in appsettings " +
                "or the ConnectionStrings__Database environment variable.");

        services.AddDbContext<PetDbContext>(options => options.UseNpgsql(connectionString));

        services.AddScoped<IUnitOfWork>(provider => provider.GetRequiredService<PetDbContext>());
        services.AddScoped<IOwnerRepository, OwnerRepository>();
        services.AddScoped<IPetRepository, PetRepository>();

        services.AddPetCareConsul(configuration);
        services.AddPetServiceApplication();
        return services;
    }
}
