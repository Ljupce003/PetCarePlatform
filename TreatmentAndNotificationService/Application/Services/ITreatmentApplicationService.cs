using TreatmentAndNotificationService.Application.Models;

namespace TreatmentAndNotificationService.Application.Services;

public interface ITreatmentApplicationService
{
    Task<List<MedicalExaminationDto>> GetMedicalHistory(Guid petId, CancellationToken ct);

    Task<MedicalExaminationDto> RecordExaminationAsync(RecordMedicalExaminationRequest request, CancellationToken ct);

    Task<List<VaccinationDto>> GetVaccinationsAsync(Guid petId, CancellationToken ct);

    Task<VaccinationDto?> GetNextVaccinationAsync(Guid petId, CancellationToken ct);

    Task<VaccinationDto> RecordVaccinationAsync(RecordVaccinationRequest request, CancellationToken ct);

    Task<List<NotificationDto>> GetNotificationsAsync(Guid ownerId, CancellationToken ct);
}