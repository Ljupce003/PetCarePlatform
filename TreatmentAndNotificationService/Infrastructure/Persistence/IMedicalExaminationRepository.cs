using TreatmentAndNotificationService.Domain.Entities;

namespace TreatmentAndNotificationService.Infrastructure.Persistence;

public interface IMedicalExaminationRepository
{
    Task AddExamination(MedicalExamination examination, CancellationToken cancellationToken);
    Task<List<MedicalExamination>> GetByPetId(Guid petId, CancellationToken cancellationToken);
}