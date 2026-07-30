using TreatmentAndNotificationService.Domain.Entities;

namespace TreatmentAndNotificationService.Infrastructure.Persistence;

public interface IVaccinationRepository
{
    Task AddVaccination(Vaccination vaccination, CancellationToken cancellationToken);
    Task<List<Vaccination>> GetByPetId(Guid petId, CancellationToken cancellationToken);
    Task<Vaccination?> GetNextVaccinationForPet(Guid petId, CancellationToken cancellationToken);

}