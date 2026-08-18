using AppointmentService.Application.Abstractions;
using AppointmentService.Application.Dtos;

namespace AppointmentService.Application.Queries;

public sealed record SearchClinicsQuery(string? Location);

public sealed class SearchClinicsHandler(IClinicRepository clinics)
{
    public async Task<IReadOnlyList<ClinicDto>> HandleAsync(SearchClinicsQuery query, CancellationToken cancellationToken) =>
        (await clinics.SearchAsync(query.Location, cancellationToken))
        .Select(clinic => clinic.ToDto())
        .ToList();
}
