using FluentValidation;
using PetService.Application.Abstractions;
using PetService.Application.Validation;
using PetService.Domain.Exceptions;

namespace PetService.Application.Commands;

public record DeleteOwnerCommand(Guid OwnerId);

public class DeleteOwnerCommandValidator : AbstractValidator<DeleteOwnerCommand>
{
    public DeleteOwnerCommandValidator()
    {
        RuleFor(command => command.OwnerId).NotEmpty();
    }
}

public class DeleteOwnerHandler(
    IOwnerRepository owners,
    IUnitOfWork unitOfWork,
    IValidator<DeleteOwnerCommand> validator)
{
    public async Task HandleAsync(DeleteOwnerCommand command, CancellationToken cancellationToken)
    {
        await validator.ValidateRequestAsync(command, cancellationToken);

        var owner = await owners.GetByIdAsync(command.OwnerId, cancellationToken)
            ?? throw new OwnerNotFoundException(command.OwnerId);

        owners.Remove(owner);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
