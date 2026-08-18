using AppointmentService.Application.Abstractions;
using AppointmentService.Application.Dtos;
using AppointmentService.Application.Exceptions;

namespace AppointmentService.Application.Commands;

public sealed record UpdateAvailabilitySlotCommand(Guid AvailabilitySlotId, DateTimeOffset StartsAtUtc, DateTimeOffset EndsAtUtc);

public sealed class UpdateAvailabilitySlotHandler(IAvailabilitySlotRepository slots, IUnitOfWork unitOfWork)
{
    public async Task<AvailableSlotDto> HandleAsync(UpdateAvailabilitySlotCommand command, CancellationToken cancellationToken)
    {
        if (command.AvailabilitySlotId == Guid.Empty || command.StartsAtUtc >= command.EndsAtUtc || command.StartsAtUtc <= DateTimeOffset.UtcNow)
            throw new ValidationException(["A valid future slot range is required."]);
        var slot = await slots.GetByIdAsync(command.AvailabilitySlotId, cancellationToken)
            ?? throw new KeyNotFoundException($"Availability slot '{command.AvailabilitySlotId}' was not found.");
        slot.Update(command.StartsAtUtc, command.EndsAtUtc);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return (await slots.SearchAvailableAsync(slot.VeterinarianId, DateOnly.FromDateTime(slot.StartsAtUtc.UtcDateTime), cancellationToken))
            .First(item => item.AvailabilitySlotId == slot.AvailabilitySlotId).ToDto();
    }
}

public sealed class DeleteAvailabilitySlotHandler(IAvailabilitySlotRepository slots, IUnitOfWork unitOfWork)
{
    public async Task HandleAsync(Guid availabilitySlotId, CancellationToken cancellationToken)
    {
        var slot = await slots.GetByIdAsync(availabilitySlotId, cancellationToken)
            ?? throw new KeyNotFoundException($"Availability slot '{availabilitySlotId}' was not found.");
        if (slot.IsBooked) throw new InvalidOperationException("A booked slot cannot be removed.");
        slots.Remove(slot);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
