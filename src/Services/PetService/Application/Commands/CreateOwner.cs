using FluentValidation;
using PetService.Application.Abstractions;
using PetService.Application.Dtos;
using PetService.Application.Validation;
using PetService.Domain.Entities;

namespace PetService.Application.Commands;

public record CreateOwnerCommand(string OwnerName, string Email, string Phone, string? Address);

public class CreateOwnerCommandValidator : AbstractValidator<CreateOwnerCommand>
{
    public CreateOwnerCommandValidator()
    {
        RuleFor(command => command.OwnerName).NotEmpty().MaximumLength(100);
        RuleFor(command => command.Email).NotEmpty().EmailAddress().MaximumLength(254);
        RuleFor(command => command.Phone)
            .NotEmpty()
            .Must(OwnerValidationRules.IsValidPhone)
            .WithMessage("Phone must contain 7 to 15 digits and may use common phone separators.");
        RuleFor(command => command.Address).MaximumLength(500);
    }
}

public class CreateOwnerHandler(
    IOwnerRepository owners,
    IUnitOfWork unitOfWork,
    IValidator<CreateOwnerCommand> validator)
{
    public async Task<OwnerDto> HandleAsync(CreateOwnerCommand command, CancellationToken cancellationToken)
    {
        await validator.ValidateRequestAsync(command, cancellationToken);

        var owner = new Owner(command.OwnerName, command.Email, command.Phone, command.Address);
        await owners.AddAsync(owner, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return owner.ToDto();
    }
}

internal static class OwnerValidationRules
{
    public static bool IsValidPhone(string? phone)
    {
        if (string.IsNullOrWhiteSpace(phone))
        {
            return false;
        }

        var normalized = phone.Trim();
        if (normalized.Any(character =>
                !char.IsDigit(character) && character is not ('+' or ' ' or '-' or '(' or ')')))
        {
            return false;
        }

        var digitCount = normalized.Count(char.IsDigit);
        var plusCount = normalized.Count(character => character == '+');
        return digitCount is >= 7 and <= 15 && (plusCount == 0 || plusCount == 1 && normalized[0] == '+');
    }
}
