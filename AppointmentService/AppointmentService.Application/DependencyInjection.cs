using AppointmentService.Application.Commands;
using AppointmentService.Application.Queries;
using Microsoft.Extensions.DependencyInjection;

namespace AppointmentService.Application;

public static class DependencyInjection
{
    /// <summary>
    /// Registers the application layer: one handler per use case, scoped so each can safely
    /// hold onto the repositories/unit of work it depends on for the lifetime of a request.
    /// The API layer only ever needs to know this one extension method.
    /// </summary>
    public static IServiceCollection AddAppointmentServiceApplication(this IServiceCollection services)
    {
        services.AddScoped<ScheduleAppointmentHandler>();
        services.AddScoped<CancelAppointmentHandler>();
        services.AddScoped<RescheduleAppointmentHandler>();
        services.AddScoped<CreateAvailabilitySlotHandler>();

        services.AddScoped<SearchClinicsHandler>();
        services.AddScoped<SearchVeterinariansHandler>();
        services.AddScoped<SearchAvailableSlotsHandler>();
        services.AddScoped<GetUpcomingAppointmentsHandler>();
        services.AddScoped<FindAvailableVeterinariansHandler>();

        return services;
    }
}
