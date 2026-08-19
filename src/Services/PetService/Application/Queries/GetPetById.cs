using FluentValidation;
using PetService.Application.Abstractions;
using PetService.Application.Dtos;
using PetService.Application.Validation;

namespace PetService.Application.Queries;

public record GetPetByIdQuery(Guid PetId);

public class GetPetByIdQueryValidator : AbstractValidator<GetPetByIdQuery>
{
    public GetPetByIdQueryValidator()
    {
        RuleFor(query => query.PetId).NotEmpty();
    }
}

public class GetPetByIdHandler(
    IPetRepository pets,
    IValidator<GetPetByIdQuery> validator)
{
    public async Task<PetDto?> HandleAsync(GetPetByIdQuery query, CancellationToken cancellationToken)
    {
        await validator.ValidateRequestAsync(query, cancellationToken);

        var pet = await pets.GetByIdAsync(query.PetId, cancellationToken);
        return pet?.ToDto();
    }
}
