using AppointmentService.Application.Abstractions;
using AppointmentService.Application.Dtos;
using AppointmentService.Application.Exceptions;
using AppointmentService.Domain.Entities;
using Microsoft.Extensions.Logging;
using Shared.AppointmentEvents;
using Shared.Messaging;

namespace AppointmentService.Application.Commands;

public sealed record ScheduleAppointmentCommand(Guid PetId, Guid OwnerId, Guid AvailabilitySlotId, string Reason);

public static class ScheduleAppointmentValidator
{
    public static void Validate(ScheduleAppointmentCommand command)
    {
        var errors = new List<string>();

        if (command.PetId == Guid.Empty)
        {
            errors.Add("PetId is required.");
        }

        if (command.OwnerId == Guid.Empty)
        {
            errors.Add("OwnerId is required.");
        }

        if (command.AvailabilitySlotId == Guid.Empty)
        {
            errors.Add("AvailabilitySlotId is required.");
        }

        if (string.IsNullOrWhiteSpace(command.Reason))
        {
            errors.Add("Reason is required.");
        }
        else if (command.Reason.Trim().Length > 500)
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
/// Books an appointment: verifies the pet with the Pet Service, reserves the requested slot
/// (this is where double-booking and expired-slot conflicts are caught, see
/// <see cref="AvailabilitySlot.Reserve"/>) and creates the appointment against it.
/// </summary>
public sealed class ScheduleAppointmentHandler(
    IAppointmentRepository appointments,
    IAvailabilitySlotRepository slots,
    IVeterinarianRepository veterinarians,
    IPetVerificationClient petVerification,
    IUnitOfWork unitOfWork,
    IIntegrationEventPublisher eventPublisher,
    ILogger<ScheduleAppointmentHandler> logger)
{
    public async Task<AppointmentDto> HandleAsync(ScheduleAppointmentCommand command, CancellationToken cancellationToken)
    {
        ScheduleAppointmentValidator.Validate(command);

        // Anti-corruption check against the Pet Service — booking depends on this succeeding.
        // If the Pet Service is briefly unreachable, the resilience handler on its HttpClient
        // (see Infrastructure/DependencyInjection) retries transparently before this call ever
        // throws.
        var verification = await petVerification.VerifyAsync(command.PetId, command.OwnerId, cancellationToken);
        if (!verification.Exists)
        {
            throw new KeyNotFoundException($"Pet '{command.PetId}' was not found.");
        }

        if (!verification.IsOwnedByOwner)
        {
            throw new PetOwnershipException(command.PetId, command.OwnerId);
        }

        var slot = await slots.GetByIdAsync(command.AvailabilitySlotId, cancellationToken)
            ?? throw new KeyNotFoundException($"Availability slot '{command.AvailabilitySlotId}' was not found.");

        var veterinarian = await veterinarians.GetByIdAsync(slot.VeterinarianId, cancellationToken)
            ?? throw new KeyNotFoundException($"Veterinarian '{slot.VeterinarianId}' was not found.");

        // Double-booking and expired-slot checks happen inside Reserve(); a failure here means
        // the appointment below is never created.
        slot.Reserve();

        var appointment = new Appointment(
            command.PetId,
            command.OwnerId,
            veterinarian.ClinicId,
            veterinarian.VeterinarianId,
            slot.AvailabilitySlotId,
            slot.StartsAtUtc,
            slot.EndsAtUtc,
            command.Reason);

        await appointments.AddAsync(appointment, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        // Published only after the commit above succeeds, so a failed booking never produces an
        // event downstream services would have to compensate for. A failed *publish*, on the
        // other hand, is logged rather than thrown -- the appointment is already booked, and
        // Kafka being briefly unreachable shouldn't turn a successful booking into an error
        // response.
        try
        {
            await eventPublisher.PublishAsync(
                PetCareTopics.Appointments,
                new AppointmentScheduledEvent(
                    Guid.NewGuid(),
                    appointment.AppointmentId,
                    appointment.PetId,
                    appointment.OwnerId,
                    appointment.VeterinarianId,
                    appointment.StartsAtUtc,
                    appointment.EndsAtUtc,
                    appointment.Reason),
                cancellationToken);
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception,
                "Appointment {AppointmentId} was booked but publishing AppointmentScheduledEvent to Kafka failed.",
                appointment.AppointmentId);
        }

        return appointment.ToDto();
    }
}
