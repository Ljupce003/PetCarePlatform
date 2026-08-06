using FluentValidation;
using PetService.Application.Abstractions;
using PetService.Application.Dtos;
using PetService.Application.Validation;

namespace PetService.Application.Queries;

public record GetOwnerQuery(Guid OwnerId);

public class GetOwnerQueryValidator : AbstractValidator<GetOwnerQuery>
{
    public GetOwnerQueryValidator()
    {
        RuleFor(query => query.OwnerId).NotEmpty();
    }
}

public class GetOwnerHandler(
    IOwnerRepository owners,
    IValidator<GetOwnerQuery> validator)
{
    public async Task<OwnerDto?> HandleAsync(GetOwnerQuery query, CancellationToken cancellationToken)
    {
        await validator.ValidateRequestAsync(query, cancellationToken);

        var owner = await owners.GetByIdAsync(query.OwnerId, cancellationToken);
        return owner?.ToDto();
    }
}
