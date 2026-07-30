using Microsoft.EntityFrameworkCore;
using TreatmentAndNotificationService.Domain.Entities;

namespace TreatmentAndNotificationService.Infrastructure.Persistence.RepoImpl;

public class VaccinationRepository: IVaccinationRepository
{
    private readonly TreatmentDbContext _context;

    // ReSharper disable once ConvertToPrimaryConstructor
    public VaccinationRepository(TreatmentDbContext context)
    {
        _context = context;
    }

    public Task AddVaccination(Vaccination vaccination, CancellationToken cancellationToken)
    {
        return _context.Vaccinations.AddAsync(vaccination, cancellationToken).AsTask();
    }

    public async Task<List<Vaccination>> GetByPetId(Guid petId, CancellationToken cancellationToken)
    {
        return await _context.Vaccinations
            .AsNoTracking()
            .Where(vac => vac.PetId == petId)
            .OrderByDescending(vac => vac.AdministeredOn)
            .ToListAsync(cancellationToken);
    }

    public async Task<Vaccination?> GetNextVaccinationForPet(Guid petId, CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        return await _context.Vaccinations
            .AsNoTracking()
            .Where(vac => vac.PetId == petId && vac.NextDueOn.HasValue && vac.NextDueOn.Value >= today)
            .OrderBy(vac => vac.NextDueOn)
            .FirstOrDefaultAsync(cancellationToken);
    }
}