using TreatmentAndNotificationService.Domain.Entities;

namespace TreatmentAndNotificationService.Domain.Repositories;

public interface IMedicalExaminationRepository
{
    Task AddAsync(MedicalExamination examination, CancellationToken cancellationToken);
    Task<MedicalExamination?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyList<MedicalExamination>> GetByPetIdAsync(Guid petId, CancellationToken cancellationToken);
    Task<IReadOnlyList<MedicalExamination>> GetByVeterinarianIdAsync(Guid veterinarianId, CancellationToken cancellationToken);
    void Remove(MedicalExamination examination);
}
