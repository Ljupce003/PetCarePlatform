using FluentValidation;
using PetService.Application.Abstractions;
using PetService.Application.Dtos;
using PetService.Application.Validation;
using PetService.Domain.Exceptions;

namespace PetService.Application.Queries;

public record GetOwnerPetsQuery(Guid OwnerId);

public class GetOwnerPetsQueryValidator : AbstractValidator<GetOwnerPetsQuery>
{
    public GetOwnerPetsQueryValidator()
    {
        RuleFor(query => query.OwnerId).NotEmpty();
    }
}

public class GetOwnerPetsHandler(
    IOwnerRepository owners,
    IPetRepository pets,
    IValidator<GetOwnerPetsQuery> validator)
{
    public async Task<IReadOnlyList<PetDto>> HandleAsync(
        GetOwnerPetsQuery query,
        CancellationToken cancellationToken)
    {
        await validator.ValidateRequestAsync(query, cancellationToken);

        _ = await owners.GetByIdAsync(query.OwnerId, cancellationToken)
            ?? throw new OwnerNotFoundException(query.OwnerId);

        return (await pets.GetByOwnerIdAsync(query.OwnerId, cancellationToken))
            .Select(pet => pet.ToDto())
            .ToList();
    }
}
