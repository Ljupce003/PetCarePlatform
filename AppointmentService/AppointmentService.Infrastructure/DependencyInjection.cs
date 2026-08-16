using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
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

        // JwtTokenService remains available only for the WebApplicationFactory "Testing"
        // environment. Real users and service-to-service calls use Keycloak.
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

        // Registers this instance in Consul on startup (deregisters on shutdown) and exposes
        // IConsulServiceResolver for the Pet Service client below.
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
        // Explicit opt-in for isolated Appointment development and tests where Pet Service is not
        // running. Docker and integrated environments use the real Pet ownership contract.
        if (bool.TryParse(configuration["PetService:UseFakeVerification"], out var useFakeVerification) && useFakeVerification)
        {
            services.AddSingleton<IPetVerificationClient, FakePetVerificationClient>();
            return;
        }

        var petServiceBaseUrl = configuration["PetService:BaseUrl"]
            ?? throw new InvalidOperationException(
                "PetService:BaseUrl is not configured. Set PetService:BaseUrl in appsettings or the " +
                "PetService__BaseUrl environment variable.");

        var keycloakAuthority = configuration["Jwt:Authority"]?.TrimEnd('/')
            ?? throw new InvalidOperationException("Jwt:Authority is required for service-to-service authentication.");
        var keycloakIssuer = configuration["Jwt:Issuer"]
            ?? throw new InvalidOperationException("Jwt:Issuer is required for service-to-service authentication.");
        var clientId = configuration["Jwt:ClientId"]
            ?? throw new InvalidOperationException("Jwt:ClientId is required for service-to-service authentication.");
        var clientSecret = configuration["Jwt:ClientSecret"]
            ?? throw new InvalidOperationException("Jwt:ClientSecret is required for service-to-service authentication.");

        services.AddOptions<KeycloakServiceAccessTokenOptions>()
            .Configure(options =>
            {
                options.ClientId = clientId;
                options.ClientSecret = clientSecret;
                options.RefreshSkewSeconds = configuration.GetValue("Jwt:ServiceTokenRefreshSkewSeconds", 30);
            })
            .Validate(options => !string.IsNullOrWhiteSpace(options.ClientId), "A Keycloak service client ID is required.")
            .Validate(options => !string.IsNullOrWhiteSpace(options.ClientSecret), "A Keycloak service client secret is required.")
            .ValidateOnStart();

        services.TryAddSingleton(TimeProvider.System);
        services.AddHttpClient("keycloak-service-token", client =>
        {
            client.BaseAddress = new Uri(keycloakAuthority + "/");
            // Docker reaches Keycloak through its internal DNS name, while tokens intentionally
            // use the browser-visible localhost issuer validated by every API.
            client.DefaultRequestHeaders.Host = new Uri(keycloakIssuer).Authority;
            client.Timeout = TimeSpan.FromSeconds(10);
        });
        services.AddSingleton<IServiceAccessTokenProvider, KeycloakServiceAccessTokenProvider>();
        services.AddTransient<ServiceAccessTokenHandler>();

        services.AddHttpClient<IPetVerificationClient, PetServiceClient>(client =>
            {
                client.BaseAddress = new Uri(petServiceBaseUrl);
            })
            .AddHttpMessageHandler<ServiceAccessTokenHandler>()
            .AddHttpMessageHandler<ServiceDiscoveryHandler>()
            // Retry with backoff + circuit breaker + timeout for transient Pet Service failures,
            // instead of a single hand-rolled retry loop.
            .AddStandardResilienceHandler();
    }
}
