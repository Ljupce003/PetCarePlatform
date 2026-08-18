using AppointmentService.Application.Abstractions;
using AppointmentService.Application.Dtos;
using AppointmentService.Application.Exceptions;
using AppointmentService.Domain.Entities;

namespace AppointmentService.Application.Commands;

public sealed record CreateAvailabilitySlotCommand(Guid VeterinarianId, DateTimeOffset StartsAtUtc, DateTimeOffset EndsAtUtc);

public static class CreateAvailabilitySlotValidator
{
    public static void Validate(CreateAvailabilitySlotCommand command)
    {
        var errors = new List<string>();

        if (command.VeterinarianId == Guid.Empty)
        {
            errors.Add("VeterinarianId is required.");
        }

        if (command.StartsAtUtc >= command.EndsAtUtc)
        {
            errors.Add("Slot end must be after slot start.");
        }

        if (command.StartsAtUtc <= DateTimeOffset.UtcNow)
        {
            errors.Add("Slot start must be in the future.");
        }

        if (errors.Count > 0)
        {
            throw new ValidationException(errors);
        }
    }
}

/// <summary>
/// Creates a new open <see cref="AvailabilitySlot"/> for an existing veterinarian -- the
/// operational counterpart to the Infrastructure layer's demo seed data, for adding slots on
/// dates beyond what was seeded (e.g. from an admin tool or the MCP server, see
/// AppointmentService.Api/Mcp/AppointmentTools.cs). Not part of the original section 8 REST spec,
/// which only ever reads slots -- clinics/admins need a way to actually open new ones.
/// </summary>
public sealed class CreateAvailabilitySlotHandler(
    IVeterinarianRepository veterinarians,
    IAvailabilitySlotRepository slots,
    IUnitOfWork unitOfWork)
{
    public async Task<AvailableSlotDto> HandleAsync(CreateAvailabilitySlotCommand command, CancellationToken cancellationToken)
    {
        CreateAvailabilitySlotValidator.Validate(command);

        var veterinarian = await veterinarians.GetByIdAsync(command.VeterinarianId, cancellationToken)
            ?? throw new KeyNotFoundException($"Veterinarian '{command.VeterinarianId}' was not found.");

        var slot = new AvailabilitySlot(veterinarian.VeterinarianId, command.StartsAtUtc, command.EndsAtUtc);

        await slots.AddAsync(slot, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        // Re-read through the existing joined search rather than adding a new repository method
        // just to fetch the clinic name for the response DTO.
        var created = (await slots.SearchAvailableAsync(veterinarian.VeterinarianId, DateOnly.FromDateTime(slot.StartsAtUtc.UtcDateTime), cancellationToken))
            .First(result => result.AvailabilitySlotId == slot.AvailabilitySlotId);

        return created.ToDto();
    }
}
