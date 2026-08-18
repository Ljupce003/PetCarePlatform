using TreatmentAndNotificationService.Application.Models;
using TreatmentAndNotificationService.Domain.Entities;

namespace TreatmentAndNotificationService.Application.Mappings;

public static class TreatmentMappings
{
    public static MedicalExaminationDto ToDto(this MedicalExamination item) => new(item.Id, item.PetId,
        item.OwnerId, item.VeterinarianId, item.AppointmentId, item.ExaminedAtUtc, item.Diagnosis.Value,
        item.TreatmentPlan.Value, item.Medications, item.NextControlAtUtc, item.Notes);

    public static VaccinationDto ToDto(this Vaccination item) => new(item.Id, item.PetId, item.OwnerId,
        item.VeterinarianId, item.VaccineName.Value, item.AdministeredOn, item.NextDueOn, item.BatchNumber);

    public static NotificationDto ToDto(this Notification item) => new(item.Id, item.OwnerId, item.PetId,
        item.Type, item.Title, item.Message, item.ScheduledForUtc, item.Status, item.CreatedAtUtc, item.SentAtUtc);
}
