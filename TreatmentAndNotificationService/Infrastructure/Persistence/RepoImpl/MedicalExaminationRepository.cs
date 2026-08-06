using Microsoft.EntityFrameworkCore;
using TreatmentAndNotificationService.Domain.Entities;
using TreatmentAndNotificationService.Domain.Repositories;

namespace TreatmentAndNotificationService.Infrastructure.Persistence.RepoImpl;

public class MedicalExaminationRepository : IMedicalExaminationRepository
{
    private readonly TreatmentDbContext _context;

    // ReSharper disable once ConvertToPrimaryConstructor
    public MedicalExaminationRepository(TreatmentDbContext context)
    {
        _context = context;
    }

    public Task AddAsync(MedicalExamination examination, CancellationToken cancellationToken)
    {
        return _context.MedicalExaminations
            .AddAsync(examination, cancellationToken)
            .AsTask();
    }

    public async Task<IReadOnlyList<MedicalExamination>> GetByPetIdAsync(Guid petId, CancellationToken cancellationToken)
    {
        return await _context.MedicalExaminations
            .AsNoTracking()
            .Where(item => item.PetId == petId)
            .OrderByDescending(item => item.ExaminedAtUtc)
            .ToListAsync(cancellationToken);
    }
}
