using AppointmentService.Application.Abstractions;

namespace AppointmentService.Application.Queries;

public sealed record FindAvailableVeterinariansQuery(DateOnly Date, string? Location, string? Specialization);

public sealed record AvailableSlotSummaryDto(Guid AvailabilitySlotId, DateTimeOffset StartsAtUtc, DateTimeOffset EndsAtUtc);

public sealed record AvailableVeterinarianDto(
    Guid VeterinarianId,
    string VeterinarianName,
    string Specialization,
    Guid ClinicId,
    string ClinicName,
    IReadOnlyList<AvailableSlotSummaryDto> AvailableSlots);

/// <summary>
/// Composite read used by the MCP "find who's free" tool (see AppointmentService.Api/Mcp/AppointmentTools.cs)
/// and reusable from anywhere else in-process. There's no dedicated repository query for "open
/// slots at clinics in a given location" -- clinics only carry their own Location, and
/// AvailableSlotSearchResult doesn't -- so this composes two existing reads instead of adding a
/// new one to IAvailabilitySlotRepository: it resolves matching clinic ids from
/// <see cref="IClinicRepository"/> (only when a location filter is given) and then filters/groups
/// the open slots for the date from <see cref="IAvailabilitySlotRepository"/> client-side.
/// </summary>
public sealed class FindAvailableVeterinariansHandler(IClinicRepository clinics, IAvailabilitySlotRepository slots)
{
    public async Task<IReadOnlyList<AvailableVeterinarianDto>> HandleAsync(
        FindAvailableVeterinariansQuery query, CancellationToken cancellationToken)
    {
        HashSet<Guid>? matchingClinicIds = null;
        if (!string.IsNullOrWhiteSpace(query.Location))
        {
            var matchingClinics = await clinics.SearchAsync(query.Location, cancellationToken);
            matchingClinicIds = matchingClinics.Select(clinic => clinic.ClinicId).ToHashSet();

            if (matchingClinicIds.Count == 0)
            {
                return [];
            }
        }

        // SearchAvailableAsync already excludes booked/expired slots (see AvailabilitySlotRepository).
        var openSlots = await slots.SearchAvailableAsync(veterinarianId: null, query.Date, cancellationToken);

        var filtered = openSlots
            .Where(slot => matchingClinicIds is null || matchingClinicIds.Contains(slot.ClinicId))
            .Where(slot => string.IsNullOrWhiteSpace(query.Specialization) ||
                           slot.Specialization.Contains(query.Specialization, StringComparison.OrdinalIgnoreCase));

        return filtered
            .GroupBy(slot => new { slot.VeterinarianId, slot.VeterinarianName, slot.Specialization, slot.ClinicId, slot.ClinicName })
            .Select(group => new AvailableVeterinarianDto(
                group.Key.VeterinarianId,
                group.Key.VeterinarianName,
                group.Key.Specialization,
                group.Key.ClinicId,
                group.Key.ClinicName,
                group.OrderBy(slot => slot.StartsAtUtc)
                    .Select(slot => new AvailableSlotSummaryDto(slot.AvailabilitySlotId, slot.StartsAtUtc, slot.EndsAtUtc))
                    .ToList()))
            .OrderBy(veterinarian => veterinarian.VeterinarianName)
            .ToList();
    }
}
