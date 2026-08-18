using TreatmentAndNotificationService.Application.Abstractions;
using TreatmentAndNotificationService.Application.Events;
using TreatmentAndNotificationService.Application.Mappings;
using TreatmentAndNotificationService.Application.Models;
using TreatmentAndNotificationService.Application.Services;
using TreatmentAndNotificationService.Domain.Entities;
using TreatmentAndNotificationService.Domain.Enums;
using TreatmentAndNotificationService.Domain.Repositories;
using TreatmentAndNotificationService.Domain.ValueObjects;

namespace TreatmentAndNotificationService.Application.Commands;

public sealed class RecordMedicalExaminationCommandHandler(
    IMedicalExaminationRepository examinations,
    IUnitOfWork unitOfWork,
    IDomainEventDispatcher eventDispatcher,
    OwnerNotificationService ownerNotifications)
    : ICommandHandler<RecordMedicalExaminationCommand, MedicalExaminationDto>
{
    public async Task<MedicalExaminationDto> HandleAsync(RecordMedicalExaminationCommand command, CancellationToken cancellationToken)
    {
        var examination = new MedicalExamination(command.PetId, command.OwnerId, command.VeterinarianId,
            command.AppointmentId, command.ExaminedAtUtc, Diagnosis.Create(command.Diagnosis),
            TreatmentPlan.Create(command.TreatmentPlan), command.Medications, command.NextControlAtUtc, command.Notes);

        await examinations.AddAsync(examination, cancellationToken);
        await eventDispatcher.DispatchAsync(examination.DequeueDomainEvents(), cancellationToken);
        await ownerNotifications.AddAsync(examination.OwnerId, examination.PetId, NotificationType.MedicalRecordCreated,
            "Medical examination added", "A veterinarian added a medical examination to your pet's care record.",
            $"medical-examination:{examination.Id}:created", cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return examination.ToDto();
    }
}
