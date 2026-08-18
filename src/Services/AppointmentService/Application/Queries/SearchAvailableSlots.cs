using AppointmentService.Application.Abstractions;
using AppointmentService.Application.Dtos;
using AppointmentService.Application.Exceptions;

namespace AppointmentService.Application.Queries;

public sealed record SearchAvailableSlotsQuery(Guid? VeterinarianId, DateOnly? Date);

public static class SearchAvailableSlotsValidator
{
    public static void Validate(SearchAvailableSlotsQuery query)
    {
        if (query.Date is { } date && date < DateOnly.FromDateTime(DateTime.UtcNow))
        {
            throw new ValidationException(["Date cannot be in the past."]);
        }
    }
}

public sealed class SearchAvailableSlotsHandler(IAvailabilitySlotRepository slots)
{
    public async Task<IReadOnlyList<AvailableSlotDto>> HandleAsync(SearchAvailableSlotsQuery query, CancellationToken cancellationToken)
    {
        SearchAvailableSlotsValidator.Validate(query);

        return (await slots.SearchAvailableAsync(query.VeterinarianId, query.Date, cancellationToken))
            .Select(result => result.ToDto())
            .ToList();
    }
}
