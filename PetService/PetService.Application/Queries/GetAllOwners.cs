using PetService.Application.Abstractions;
using PetService.Application.Dtos;

namespace PetService.Application.Queries;

public record GetAllOwnersQuery;

public class GetAllOwnersHandler(IOwnerRepository owners)
{
    public async Task<IReadOnlyList<OwnerDto>> HandleAsync(
        GetAllOwnersQuery query,
        CancellationToken cancellationToken) =>
        (await owners.GetAllAsync(cancellationToken))
            .Select(owner => owner.ToDto())
            .ToList();
}
