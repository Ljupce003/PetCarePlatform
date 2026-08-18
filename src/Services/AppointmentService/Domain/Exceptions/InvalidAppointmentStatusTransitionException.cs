using AppointmentService.Domain.Enums;

namespace AppointmentService.Domain.Exceptions;

public class InvalidAppointmentStatusTransitionException : Exception
{
    public InvalidAppointmentStatusTransitionException(Guid appointmentId, AppointmentStatus currentStatus, string attemptedAction)
        : base($"Appointment '{appointmentId}' cannot perform '{attemptedAction}' while its status is '{currentStatus}'. " +
               "Only scheduled appointments can change status.")
    {
        AppointmentId = appointmentId;
        CurrentStatus = currentStatus;
        AttemptedAction = attemptedAction;
    }

    public Guid AppointmentId { get; }
    public AppointmentStatus CurrentStatus { get; }
    public string AttemptedAction { get; }
}
