using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using AppointmentService.Application;
using AppointmentService.Application.Abstractions;
using AppointmentService.Infrastructure.Clients;
using AppointmentService.Infrastructure.Persistence;
using AppointmentService.Infrastructure.Security;
using Shared.Messaging;
using Shared.ServiceDiscovery;

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

        // The DbContext itself implements IUnitOfWork (see AppointmentDbContext), so every
        // handler that loaded its entities through one of the repositories below commits
        // through the very same context.
        services.AddScoped<IUnitOfWork>(provider => provider.GetRequiredService<AppointmentDbContext>());
        services.AddScoped<IClinicRepository, ClinicRepository>();
        services.AddScoped<IVeterinarianRepository, VeterinarianRepository>();
        services.AddScoped<IAvailabilitySlotRepository, AvailabilitySlotRepository>();
        services.AddScoped<IAppointmentRepository, AppointmentRepository>();

        // Local JWTs are only used by the isolated WebApplicationFactory test environment.
        services.Configure<JwtOptions>(configuration.GetSection("Jwt"));
        services.AddSingleton<JwtTokenService>();

        // The only login path AuthController has: proxies to Keycloak's own token endpoint.
        services.AddHttpClient<KeycloakAuthClient>((provider, client) =>
        {
            var authority = provider.GetRequiredService<IConfiguration>()["Jwt:Authority"]?.TrimEnd('/')
                ?? throw new InvalidOperationException("Jwt:Authority is required.");
            client.BaseAddress = new Uri(authority + "/");
            client.Timeout = TimeSpan.FromSeconds(10);
        });

        services.AddPetCareConsul(configuration);
        AddPetServiceClient(services, configuration);

        // Publishes AppointmentScheduled/Cancelled/Rescheduled to Kafka (topic: petcare.appointments)
        // so Treatment & Notification Service can react to them.
        services.AddPetCareKafka(configuration);

        services.AddAppointmentServiceApplication();
        return services;
    }

    private static void AddPetServiceClient(IServiceCollection services, IConfiguration configuration)
    {
        var petServiceBaseUrl = configuration["PetService:BaseUrl"]
            ?? throw new InvalidOperationException(
                "PetService:BaseUrl is not configured. Set PetService:BaseUrl in appsettings or the " +
                "PetService__BaseUrl environment variable.");

        services.AddHttpClient("keycloak-service-token", (provider, client) =>
        {
            var authority = provider.GetRequiredService<IConfiguration>()["Jwt:Authority"]?.TrimEnd('/')
                ?? throw new InvalidOperationException("Jwt:Authority is required.");
            client.BaseAddress = new Uri(authority + "/");
            client.Timeout = TimeSpan.FromSeconds(10);
        });

        services.AddHttpClient<KeycloakAdminClient>((provider, client) =>
        {
            var authority = provider.GetRequiredService<IConfiguration>()["Jwt:Authority"]?.TrimEnd('/')
                ?? throw new InvalidOperationException("Jwt:Authority is required.");
            var realmsIndex = authority.IndexOf("/realms/", StringComparison.OrdinalIgnoreCase);
            var keycloakBaseUrl = realmsIndex >= 0 ? authority[..realmsIndex] : authority;
            client.BaseAddress = new Uri(keycloakBaseUrl.TrimEnd('/') + "/");
            client.Timeout = TimeSpan.FromSeconds(10);
        });
        services.AddSingleton<IServiceAccessTokenProvider, KeycloakServiceAccessTokenProvider>();
        services.AddTransient<ServiceAccessTokenHandler>();

        services.AddHttpClient<IPetVerificationClient, PetServiceClient>(client =>
            {
                client.BaseAddress = new Uri(petServiceBaseUrl);
            })
            .AddHttpMessageHandler<ServiceDiscoveryHandler>()
            .AddHttpMessageHandler<ServiceAccessTokenHandler>()
            .AddStandardResilienceHandler();
    }
}
