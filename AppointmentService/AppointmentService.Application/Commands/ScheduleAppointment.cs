using AppointmentService.Application.Abstractions;
using AppointmentService.Application.Dtos;
using AppointmentService.Application.Exceptions;
using AppointmentService.Domain.Entities;

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
/// Books an appointment: reserves the requested slot (this is where double-booking and
/// expired-slot conflicts are caught, see <see cref="AvailabilitySlot.Reserve"/>) and creates
/// the appointment against it.
/// </summary>
/// <remarks>
/// Verifying that the pet exists and belongs to the owner is deliberately not done here yet —
/// that anti-corruption check against the Pet Service is a separate piece of work (REST
/// integration with Pet Service) and will slot in right before the slot is reserved.
/// </remarks>
public sealed class ScheduleAppointmentHandler(
    IAppointmentRepository appointments,
    IAvailabilitySlotRepository slots,
    IVeterinarianRepository veterinarians,
    IUnitOfWork unitOfWork)
{
    public async Task<AppointmentDto> HandleAsync(ScheduleAppointmentCommand command, CancellationToken cancellationToken)
    {
        ScheduleAppointmentValidator.Validate(command);

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

        return appointment.ToDto();
    }
}
