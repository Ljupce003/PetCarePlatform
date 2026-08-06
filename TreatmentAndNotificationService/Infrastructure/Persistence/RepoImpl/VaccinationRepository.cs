using Microsoft.EntityFrameworkCore;
using TreatmentAndNotificationService.Domain.Entities;
using TreatmentAndNotificationService.Domain.Repositories;

namespace TreatmentAndNotificationService.Infrastructure.Persistence.RepoImpl;

public class VaccinationRepository: IVaccinationRepository
{
    private readonly TreatmentDbContext _context;

    // ReSharper disable once ConvertToPrimaryConstructor
    public VaccinationRepository(TreatmentDbContext context)
    {
        _context = context;
    }

    public Task AddAsync(Vaccination vaccination, CancellationToken cancellationToken)
    {
        return _context.Vaccinations.AddAsync(vaccination, cancellationToken).AsTask();
    }

    public async Task<IReadOnlyList<Vaccination>> GetByPetIdAsync(Guid petId, CancellationToken cancellationToken)
    {
        return await _context.Vaccinations
            .AsNoTracking()
            .Where(vac => vac.PetId == petId)
            .OrderByDescending(vac => vac.AdministeredOn)
            .ToListAsync(cancellationToken);
    }

    public async Task<Vaccination?> GetNextForPetAsync(Guid petId, DateOnly onOrAfter, CancellationToken cancellationToken)
    {
        return await _context.Vaccinations
            .AsNoTracking()
            .Where(vac => vac.PetId == petId && vac.NextDueOn.HasValue && vac.NextDueOn.Value >= onOrAfter)
            .OrderBy(vac => vac.NextDueOn)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
