using PetService.Application.Abstractions;
using PetService.Application.Dtos;

namespace PetService.Application.Queries;

public record GetAllPetsQuery;

public class GetAllPetsHandler(IPetRepository pets)
{
    public async Task<IReadOnlyList<PetDto>> HandleAsync(
        GetAllPetsQuery query,
        CancellationToken cancellationToken) =>
        (await pets.GetAllAsync(cancellationToken))
            .Select(pet => pet.ToDto())
            .ToList();
}
