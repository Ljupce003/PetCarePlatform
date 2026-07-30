using TreatmentAndNotificationService.Application.Models;

namespace TreatmentAndNotificationService.Application.Services.Impl;

public class TreatmentApplicationService: ITreatmentApplicationService
{
    
    public Task<List<MedicalExaminationDto>> GetMedicalHistory(Guid petId, CancellationToken ct)
    {
        throw new NotImplementedException();
    }

    public Task<MedicalExaminationDto> RecordExaminationAsync(RecordMedicalExaminationRequest request, CancellationToken ct)
    {
        throw new NotImplementedException();
    }

    public Task<List<VaccinationDto>> GetVaccinationsAsync(Guid petId, CancellationToken ct)
    {
        throw new NotImplementedException();
    }

    public Task<VaccinationDto?> GetNextVaccinationAsync(Guid petId, CancellationToken ct)
    {
        throw new NotImplementedException();
    }

    public Task<VaccinationDto> RecordVaccinationAsync(RecordVaccinationRequest request, CancellationToken ct)
    {
        throw new NotImplementedException();
    }

    public Task<List<NotificationDto>> GetNotificationsAsync(Guid ownerId, CancellationToken ct)
    {
        throw new NotImplementedException();
    }
}