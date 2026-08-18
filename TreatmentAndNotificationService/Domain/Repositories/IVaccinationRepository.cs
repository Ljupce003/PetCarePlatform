using TreatmentAndNotificationService.Domain.Entities;

namespace TreatmentAndNotificationService.Domain.Repositories;

public interface IVaccinationRepository
{
    Task AddAsync(Vaccination vaccination, CancellationToken cancellationToken);
    Task<Vaccination?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyList<Vaccination>> GetByPetIdAsync(Guid petId, CancellationToken cancellationToken);
    Task<IReadOnlyList<Vaccination>> GetByVeterinarianIdAsync(Guid veterinarianId, CancellationToken cancellationToken);
    void Remove(Vaccination vaccination);
    Task<Vaccination?> GetNextForPetAsync(Guid petId, DateOnly onOrAfter, CancellationToken cancellationToken);
}
