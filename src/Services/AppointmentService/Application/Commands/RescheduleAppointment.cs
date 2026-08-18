using AppointmentService.Application.Abstractions;
using AppointmentService.Application.Dtos;
using AppointmentService.Application.Exceptions;
using Microsoft.Extensions.Logging;
using Shared.AppointmentEvents;
using Shared.Messaging;

namespace AppointmentService.Application.Commands;

public sealed record RescheduleAppointmentCommand(Guid AppointmentId, Guid NewAvailabilitySlotId);

public static class RescheduleAppointmentValidator
{
    public static void Validate(RescheduleAppointmentCommand command)
    {
        var errors = new List<string>();

        if (command.AppointmentId == Guid.Empty)
        {
            errors.Add("AppointmentId is required.");
        }

        if (command.NewAvailabilitySlotId == Guid.Empty)
        {
            errors.Add("NewAvailabilitySlotId is required.");
        }

        if (errors.Count > 0)
        {
            throw new ValidationException(errors);
        }
    }
}

/// <summary>
/// Moves a still-scheduled appointment onto a different slot. The new slot is reserved before
/// the old one is released, so a failed reservation (already booked / expired) never leaves the
/// appointment holding no slot at all.
/// </summary>
public sealed class RescheduleAppointmentHandler(
    IAppointmentRepository appointments,
    IAvailabilitySlotRepository slots,
    IUnitOfWork unitOfWork,
    IIntegrationEventPublisher eventPublisher,
    ILogger<RescheduleAppointmentHandler> logger)
{
    public async Task<AppointmentDto> HandleAsync(RescheduleAppointmentCommand command, CancellationToken cancellationToken)
    {
        RescheduleAppointmentValidator.Validate(command);

        var appointment = await appointments.GetByIdAsync(command.AppointmentId, cancellationToken)
            ?? throw new KeyNotFoundException($"Appointment '{command.AppointmentId}' was not found.");

        if (appointment.AvailabilitySlotId == command.NewAvailabilitySlotId)
        {
            return appointment.ToDto();
        }

        var newSlot = await slots.GetByIdAsync(command.NewAvailabilitySlotId, cancellationToken)
            ?? throw new KeyNotFoundException($"Availability slot '{command.NewAvailabilitySlotId}' was not found.");

        var oldSlot = await slots.GetByIdAsync(appointment.AvailabilitySlotId, cancellationToken)
            ?? throw new KeyNotFoundException($"Availability slot '{appointment.AvailabilitySlotId}' was not found.");

        newSlot.Reserve();
        oldSlot.Release();

        appointment.Reschedule(newSlot.AvailabilitySlotId, newSlot.VeterinarianId, newSlot.StartsAtUtc, newSlot.EndsAtUtc);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        try
        {
            await eventPublisher.PublishAsync(
                PetCareTopics.Appointments,
                new AppointmentRescheduledEvent(
                    Guid.NewGuid(),
                    appointment.AppointmentId,
                    appointment.PetId,
                    appointment.OwnerId,
                    appointment.VeterinarianId,
                    appointment.StartsAtUtc,
                    appointment.EndsAtUtc),
                cancellationToken);
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception,
                "Appointment {AppointmentId} was rescheduled but publishing AppointmentRescheduledEvent to Kafka failed.",
                appointment.AppointmentId);
        }

        return appointment.ToDto();
    }
}
