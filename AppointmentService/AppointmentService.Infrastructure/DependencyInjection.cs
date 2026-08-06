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

        // JwtTokenService issues/is configured for the tokens both /auth/* endpoints (Api layer)
        // and LocalServiceAccessTokenProvider (below) hand out. See Security/JwtOptions.cs.
        services.Configure<JwtOptions>(configuration.GetSection("Jwt"));
        services.AddSingleton<JwtTokenService>();

        AddPetServiceClient(services, configuration);

        // Registers this instance in Consul on startup (deregisters on shutdown) and exposes
        // IConsulServiceResolver for looking up other services once they register too.
        services.AddPetCareConsul(configuration);

        // Publishes AppointmentScheduled/Cancelled/Rescheduled to Kafka (topic: petcare.appointments)
        // so Treatment & Notification Service can react to them.
        services.AddPetCareKafka(configuration);

        services.AddAppointmentServiceApplication();
        return services;
    }

    private static void AddPetServiceClient(IServiceCollection services, IConfiguration configuration)
    {
        // Pet Service doesn't have its /api/pets/{id}/exists endpoint (or any seeded pets/owners)
        // yet, so booking through this service's own Swagger would otherwise always fail
        // verification. PetService:UseFakeVerification (true in appsettings.Development.json,
        // false everywhere else) swaps in FakePetVerificationClient instead -- delete this switch
        // once Pet Service's real endpoint exists.
        if (bool.TryParse(configuration["PetService:UseFakeVerification"], out var useFakeVerification) && useFakeVerification)
        {
            services.AddSingleton<IPetVerificationClient, FakePetVerificationClient>();
            return;
        }

        var petServiceBaseUrl = configuration["PetService:BaseUrl"]
            ?? throw new InvalidOperationException(
                "PetService:BaseUrl is not configured. Set PetService:BaseUrl in appsettings or the " +
                "PetService__BaseUrl environment variable.");

        // Attaches a real (locally-signed) token to every Pet Service call — see
        // LocalServiceAccessTokenProvider for what changes once Keycloak exists.
        services.AddSingleton<IServiceAccessTokenProvider, LocalServiceAccessTokenProvider>();
        services.AddTransient<ServiceAccessTokenHandler>();

        services.AddHttpClient<IPetVerificationClient, PetServiceClient>(client =>
            {
                client.BaseAddress = new Uri(petServiceBaseUrl);
            })
            .AddHttpMessageHandler<ServiceAccessTokenHandler>()
            // Retry with backoff + circuit breaker + timeout for transient Pet Service failures,
            // instead of a single hand-rolled retry loop.
            .AddStandardResilienceHandler();
    }
}
