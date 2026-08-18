namespace TreatmentAndNotificationService.Domain.Enums;

public enum NotificationType
{
    AppointmentScheduled = 1,
    AppointmentCancelled = 2,
    AppointmentRescheduled = 3,
    FollowUpReminder = 4,
    VaccinationReminder = 5,
    MedicalRecordCreated = 6,
    MedicalRecordUpdated = 7,
    MedicalRecordDeleted = 8,
    VaccinationRecorded = 9,
    VaccinationUpdated = 10,
    VaccinationDeleted = 11
}
