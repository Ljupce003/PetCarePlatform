using Microsoft.EntityFrameworkCore;
using TreatmentAndNotificationService.Domain.Entities;

namespace TreatmentAndNotificationService.Infrastructure.Persistence.RepoImpl;

public class MedicalExaminationRepository : IMedicalExaminationRepository
{
    private readonly TreatmentDbContext _context;

    // ReSharper disable once ConvertToPrimaryConstructor
    public MedicalExaminationRepository(TreatmentDbContext context)
    {
        _context = context;
    }

    public Task AddExamination(MedicalExamination examination, CancellationToken cancellationToken)
    {
        return _context.MedicalExaminations
            .AddAsync(examination, cancellationToken)
            .AsTask();
    }

    public async Task<List<MedicalExamination>> GetByPetId(Guid petId, CancellationToken cancellationToken)
    {
        return await _context.MedicalExaminations
            .AsNoTracking()
            .Where(item => item.PetId == petId)
            .OrderByDescending(item => item.ExaminedAtUtc)
            .ToListAsync(cancellationToken);
    }
}