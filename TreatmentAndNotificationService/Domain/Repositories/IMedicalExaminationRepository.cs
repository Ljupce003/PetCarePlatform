using TreatmentAndNotificationService.Domain.Entities;

namespace TreatmentAndNotificationService.Domain.Repositories;

public interface IMedicalExaminationRepository
{
    Task AddAsync(MedicalExamination examination, CancellationToken cancellationToken);
    Task<IReadOnlyList<MedicalExamination>> GetByPetIdAsync(Guid petId, CancellationToken cancellationToken);
}
