using Shared.AppointmentEvents;

namespace TreatmentAndNotificationService.Application.Services.Impl;

public class AppointmentNotificationApplicationService : IAppointmentNotificationApplicationService
{
    public Task HandleAsync(AppointmentScheduledEvent message, CancellationToken ct)
    {
        throw new NotImplementedException();
    }

    public Task HandleAsync(AppointmentCancelledEvent message, CancellationToken ct)
    {
        throw new NotImplementedException();
    }

    public Task HandleAsync(AppointmentRescheduledEvent message, CancellationToken ct)
    {
        throw new NotImplementedException();
    }
}