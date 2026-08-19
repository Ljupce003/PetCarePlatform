using FluentValidation;
using PetService.Application.Abstractions;
using PetService.Application.Dtos;
using PetService.Application.Validation;

namespace PetService.Application.Queries;

public record CheckPetOwnershipQuery(Guid PetId, Guid OwnerId);

public class CheckPetOwnershipQueryValidator : AbstractValidator<CheckPetOwnershipQuery>
{
    public CheckPetOwnershipQueryValidator()
    {
        RuleFor(query => query.PetId).NotEmpty();
        RuleFor(query => query.OwnerId).NotEmpty();
    }
}

public class CheckPetOwnershipHandler(
    IPetRepository pets,
    IValidator<CheckPetOwnershipQuery> validator)
{
    public async Task<PetOwnershipDto> HandleAsync(
        CheckPetOwnershipQuery query,
        CancellationToken cancellationToken)
    {
        await validator.ValidateRequestAsync(query, cancellationToken);

        var pet = await pets.GetByIdAsync(query.PetId, cancellationToken);
        return new PetOwnershipDto(pet is not null, pet?.OwnerId == query.OwnerId);
    }
}
