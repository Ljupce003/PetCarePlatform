using AppointmentService.Application.Abstractions;
using AppointmentService.Application.Dtos;
using AppointmentService.Application.Exceptions;
using Shared.AppointmentEvents;
using Shared.Messaging;

namespace AppointmentService.Application.Commands;

public sealed record CancelAppointmentCommand(Guid AppointmentId, string? Reason);

public static class CancelAppointmentValidator
{
    public static void Validate(CancelAppointmentCommand command)
    {
        var errors = new List<string>();

        if (command.AppointmentId == Guid.Empty)
        {
            errors.Add("AppointmentId is required.");
        }

        if (command.Reason is { Length: > 500 })
        {
            errors.Add("Reason must be 500 characters or fewer.");
        }

        if (errors.Count > 0)
        {
            throw new ValidationException(errors);
        }
    }
}

/// <summary>
/// Cancels a still-scheduled appointment (<see cref="Domain.Entities.Appointment.Cancel"/> is
/// where the "only a scheduled appointment can be cancelled" rule lives) and frees its slot back
/// up so another owner can book it.
/// </summary>
public sealed class CancelAppointmentHandler(
    IAppointmentRepository appointments,
    IAvailabilitySlotRepository slots,
    IUnitOfWork unitOfWork,
    IIntegrationEventPublisher eventPublisher)
{
    public async Task<AppointmentDto> HandleAsync(CancelAppointmentCommand command, CancellationToken cancellationToken)
    {
        CancelAppointmentValidator.Validate(command);

        var appointment = await appointments.GetByIdAsync(command.AppointmentId, cancellationToken)
            ?? throw new KeyNotFoundException($"Appointment '{command.AppointmentId}' was not found.");

        var slot = await slots.GetByIdAsync(appointment.AvailabilitySlotId, cancellationToken)
            ?? throw new KeyNotFoundException($"Availability slot '{appointment.AvailabilitySlotId}' was not found.");

        appointment.Cancel(command.Reason);
        slot.Release();

        await unitOfWork.SaveChangesAsync(cancellationToken);

        await eventPublisher.PublishAsync(
            PetCareTopics.Appointments,
            new AppointmentCancelledEvent(
                Guid.NewGuid(),
                appointment.AppointmentId,
                appointment.PetId,
                appointment.OwnerId,
                DateTimeOffset.UtcNow,
                appointment.CancellationReason),
            cancellationToken);

        return appointment.ToDto();
    }
}
