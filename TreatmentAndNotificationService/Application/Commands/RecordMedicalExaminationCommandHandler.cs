using TreatmentAndNotificationService.Application.Abstractions;
using TreatmentAndNotificationService.Application.Events;
using TreatmentAndNotificationService.Application.Mappings;
using TreatmentAndNotificationService.Application.Models;
using TreatmentAndNotificationService.Domain.Entities;
using TreatmentAndNotificationService.Domain.Repositories;
using TreatmentAndNotificationService.Domain.ValueObjects;

namespace TreatmentAndNotificationService.Application.Commands;

public sealed class RecordMedicalExaminationCommandHandler(
    IMedicalExaminationRepository examinations,
    IUnitOfWork unitOfWork,
    IDomainEventDispatcher eventDispatcher)
    : ICommandHandler<RecordMedicalExaminationCommand, MedicalExaminationDto>
{
    public async Task<MedicalExaminationDto> HandleAsync(RecordMedicalExaminationCommand command, CancellationToken cancellationToken)
    {
        var examination = new MedicalExamination(command.PetId, command.OwnerId, command.VeterinarianId,
            command.AppointmentId, command.ExaminedAtUtc, Diagnosis.Create(command.Diagnosis),
            TreatmentPlan.Create(command.TreatmentPlan), command.Medications, command.NextControlAtUtc, command.Notes);

        await examinations.AddAsync(examination, cancellationToken);
        await eventDispatcher.DispatchAsync(examination.DequeueDomainEvents(), cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return examination.ToDto();
    }
}
