using TreatmentAndNotificationService.Application.Abstractions;
using TreatmentAndNotificationService.Application.Mappings;
using TreatmentAndNotificationService.Application.Models;
using TreatmentAndNotificationService.Domain.Repositories;

namespace TreatmentAndNotificationService.Application.Queries;

public sealed class GetMedicalHistoryQueryHandler(IMedicalExaminationRepository examinations)
    : IQueryHandler<GetMedicalHistoryQuery, IReadOnlyList<MedicalExaminationDto>>
{
    public async Task<IReadOnlyList<MedicalExaminationDto>> HandleAsync(GetMedicalHistoryQuery query, CancellationToken cancellationToken) =>
        (await examinations.GetByPetIdAsync(query.PetId, cancellationToken)).Select(item => item.ToDto()).ToList();
}

public sealed class GetVaccinationHistoryQueryHandler(IVaccinationRepository vaccinations)
    : IQueryHandler<GetVaccinationHistoryQuery, IReadOnlyList<VaccinationDto>>
{
    public async Task<IReadOnlyList<VaccinationDto>> HandleAsync(GetVaccinationHistoryQuery query, CancellationToken cancellationToken) =>
        (await vaccinations.GetByPetIdAsync(query.PetId, cancellationToken)).Select(item => item.ToDto()).ToList();
}

public sealed class GetNextVaccinationQueryHandler(IVaccinationRepository vaccinations)
    : IQueryHandler<GetNextVaccinationQuery, VaccinationDto?>
{
    public async Task<VaccinationDto?> HandleAsync(GetNextVaccinationQuery query, CancellationToken cancellationToken) =>
        (await vaccinations.GetNextForPetAsync(query.PetId, DateOnly.FromDateTime(DateTime.UtcNow), cancellationToken))?.ToDto();
}

public sealed class GetOwnerNotificationsQueryHandler(INotificationRepository notifications)
    : IQueryHandler<GetOwnerNotificationsQuery, IReadOnlyList<NotificationDto>>
{
    public async Task<IReadOnlyList<NotificationDto>> HandleAsync(GetOwnerNotificationsQuery query, CancellationToken cancellationToken) =>
        (await notifications.GetByOwnerIdAsync(query.OwnerId, cancellationToken)).Select(item => item.ToDto()).ToList();
}
