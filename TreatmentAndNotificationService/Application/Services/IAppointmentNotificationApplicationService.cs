namespace TreatmentAndNotificationService.Application.Services;

using Shared.AppointmentEvents;

public interface IAppointmentNotificationApplicationService
{
    Task HandleAsync(AppointmentScheduledEvent message, CancellationToken ct);
    Task HandleAsync(AppointmentCancelledEvent message, CancellationToken ct);
    Task HandleAsync(AppointmentRescheduledEvent message, CancellationToken ct);
}