using FluentValidation;
using PetService.Application.Abstractions;
using PetService.Application.Dtos;
using PetService.Application.Validation;
using PetService.Domain.Exceptions;

namespace PetService.Application.Commands;

public record UpdateOwnerCommand(
    Guid OwnerId,
    string OwnerName,
    string Email,
    string Phone,
    string? Address);

public class UpdateOwnerCommandValidator : AbstractValidator<UpdateOwnerCommand>
{
    public UpdateOwnerCommandValidator()
    {
        RuleFor(command => command.OwnerId).NotEmpty();
        RuleFor(command => command.OwnerName).NotEmpty().MaximumLength(100);
        RuleFor(command => command.Email).NotEmpty().EmailAddress().MaximumLength(254);
        RuleFor(command => command.Phone)
            .NotEmpty()
            .Must(OwnerValidationRules.IsValidPhone)
            .WithMessage("Phone must contain 7 to 15 digits and may use common phone separators.");
        RuleFor(command => command.Address).MaximumLength(500);
    }
}

public class UpdateOwnerHandler(
    IOwnerRepository owners,
    IUnitOfWork unitOfWork,
    IValidator<UpdateOwnerCommand> validator)
{
    public async Task<OwnerDto> HandleAsync(UpdateOwnerCommand command, CancellationToken cancellationToken)
    {
        await validator.ValidateRequestAsync(command, cancellationToken);

        var owner = await owners.GetByIdAsync(command.OwnerId, cancellationToken)
            ?? throw new OwnerNotFoundException(command.OwnerId);

        owner.Update(command.OwnerName, command.Email, command.Phone, command.Address);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return owner.ToDto();
    }
}
