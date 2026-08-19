using AppointmentService.Application.Abstractions;
using AppointmentService.Application.Dtos;

namespace AppointmentService.Application.Queries;

public sealed record SearchVeterinariansQuery(Guid? ClinicId, string? Specialization);

public sealed class SearchVeterinariansHandler(IVeterinarianRepository veterinarians)
{
    public async Task<IReadOnlyList<VeterinarianDto>> HandleAsync(SearchVeterinariansQuery query, CancellationToken cancellationToken) =>
        (await veterinarians.SearchAsync(query.ClinicId, query.Specialization, cancellationToken))
        .Select(veterinarian => veterinarian.ToDto())
        .ToList();
}
