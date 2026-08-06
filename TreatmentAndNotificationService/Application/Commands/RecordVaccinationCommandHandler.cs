using TreatmentAndNotificationService.Application.Abstractions;
using TreatmentAndNotificationService.Application.Events;
using TreatmentAndNotificationService.Application.Mappings;
using TreatmentAndNotificationService.Application.Models;
using TreatmentAndNotificationService.Domain.Entities;
using TreatmentAndNotificationService.Domain.Repositories;
using TreatmentAndNotificationService.Domain.ValueObjects;

namespace TreatmentAndNotificationService.Application.Commands;

public sealed class RecordVaccinationCommandHandler(
    IVaccinationRepository vaccinations,
    IUnitOfWork unitOfWork,
    IDomainEventDispatcher eventDispatcher)
    : ICommandHandler<RecordVaccinationCommand, VaccinationDto>
{
    public async Task<VaccinationDto> HandleAsync(RecordVaccinationCommand command, CancellationToken cancellationToken)
    {
        var vaccination = new Vaccination(command.PetId, command.OwnerId, command.VeterinarianId,
            VaccineName.Create(command.VaccineName), VaccinationSchedule.Create(command.AdministeredOn, command.NextDueOn),
            command.BatchNumber);

        await vaccinations.AddAsync(vaccination, cancellationToken);
        await eventDispatcher.DispatchAsync(vaccination.DequeueDomainEvents(), cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return vaccination.ToDto();
    }
}
