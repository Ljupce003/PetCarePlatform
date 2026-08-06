using Microsoft.Extensions.DependencyInjection;
using TreatmentAndNotificationService.Application.Abstractions;
using TreatmentAndNotificationService.Application.Commands;
using TreatmentAndNotificationService.Application.Events;
using TreatmentAndNotificationService.Application.Models;
using TreatmentAndNotificationService.Application.Queries;

namespace TreatmentAndNotificationService.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddTreatmentApplication(this IServiceCollection services)
    {
        services.AddScoped<IDomainEventDispatcher, DomainEventDispatcher>();
        services.AddScoped<FollowUpReminderRequestedHandler>();
        services.AddScoped<VaccinationReminderRequestedHandler>();
        services.AddScoped<ICommandHandler<RecordMedicalExaminationCommand, MedicalExaminationDto>, RecordMedicalExaminationCommandHandler>();
        services.AddScoped<ICommandHandler<RecordVaccinationCommand, VaccinationDto>, RecordVaccinationCommandHandler>();
        services.AddScoped<ICommandHandler<CreateNotificationCommand, NotificationDto>, CreateNotificationCommandHandler>();
        services.AddScoped<IQueryHandler<GetMedicalHistoryQuery, IReadOnlyList<MedicalExaminationDto>>, GetMedicalHistoryQueryHandler>();
        services.AddScoped<IQueryHandler<GetVaccinationHistoryQuery, IReadOnlyList<VaccinationDto>>, GetVaccinationHistoryQueryHandler>();
        services.AddScoped<IQueryHandler<GetNextVaccinationQuery, VaccinationDto?>, GetNextVaccinationQueryHandler>();
        services.AddScoped<IQueryHandler<GetOwnerNotificationsQuery, IReadOnlyList<NotificationDto>>, GetOwnerNotificationsQueryHandler>();
        return services;
    }
}
