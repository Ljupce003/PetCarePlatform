using TreatmentAndNotificationService.Domain.Entities;

namespace TreatmentAndNotificationService.Domain.Repositories;

public interface IVaccinationRepository
{
    Task AddAsync(Vaccination vaccination, CancellationToken cancellationToken);
    Task<IReadOnlyList<Vaccination>> GetByPetIdAsync(Guid petId, CancellationToken cancellationToken);
    Task<Vaccination?> GetNextForPetAsync(Guid petId, DateOnly onOrAfter, CancellationToken cancellationToken);
}
