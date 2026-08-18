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

public sealed class RecordVaccinationCommandHandler(
    IVaccinationRepository vaccinations,
    IUnitOfWork unitOfWork,
    IDomainEventDispatcher eventDispatcher,
    OwnerNotificationService ownerNotifications)
    : ICommandHandler<RecordVaccinationCommand, VaccinationDto>
{
    public async Task<VaccinationDto> HandleAsync(RecordVaccinationCommand command, CancellationToken cancellationToken)
    {
        var vaccination = new Vaccination(command.PetId, command.OwnerId, command.VeterinarianId,
            VaccineName.Create(command.VaccineName), VaccinationSchedule.Create(command.AdministeredOn, command.NextDueOn),
            command.BatchNumber);

        await vaccinations.AddAsync(vaccination, cancellationToken);
        await eventDispatcher.DispatchAsync(vaccination.DequeueDomainEvents(), cancellationToken);
        await ownerNotifications.AddAsync(vaccination.OwnerId, vaccination.PetId, NotificationType.VaccinationRecorded,
            "Vaccination added", $"{vaccination.VaccineName.Value} was added to your pet's vaccination record.",
            $"vaccination:{vaccination.Id}:created", cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return vaccination.ToDto();
    }
}
