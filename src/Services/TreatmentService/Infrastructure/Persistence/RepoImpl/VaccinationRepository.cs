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

    public Task<Vaccination?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        _context.Vaccinations.FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

    public void Remove(Vaccination vaccination) => _context.Vaccinations.Remove(vaccination);

    public async Task<IReadOnlyList<Vaccination>> GetByPetIdAsync(Guid petId, CancellationToken cancellationToken)
    {
        return await _context.Vaccinations
            .AsNoTracking()
            .Where(vac => vac.PetId == petId)
            .OrderByDescending(vac => vac.AdministeredOn)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Vaccination>> GetByVeterinarianIdAsync(Guid veterinarianId, CancellationToken cancellationToken)
    {
        return await _context.Vaccinations
            .AsNoTracking()
            .Where(vaccination => vaccination.VeterinarianId == veterinarianId)
            .OrderByDescending(vaccination => vaccination.AdministeredOn)
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
