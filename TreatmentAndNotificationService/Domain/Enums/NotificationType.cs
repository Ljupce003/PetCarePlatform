namespace TreatmentAndNotificationService.Domain.Enums;

public enum NotificationType
{
    AppointmentScheduled = 1,
    AppointmentCancelled = 2,
    AppointmentRescheduled = 3,
    FollowUpReminder = 4,
    VaccinationReminder = 5
}