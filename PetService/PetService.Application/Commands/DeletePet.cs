using FluentValidation;
using PetService.Application.Abstractions;
using PetService.Application.Validation;

namespace PetService.Application.Commands;

public record DeletePetCommand(Guid PetId);

public class DeletePetCommandValidator : AbstractValidator<DeletePetCommand>
{
    public DeletePetCommandValidator()
    {
        RuleFor(command => command.PetId).NotEmpty();
    }
}

public class DeletePetHandler(
    IPetRepository pets,
    IUnitOfWork unitOfWork,
    IValidator<DeletePetCommand> validator)
{
    public async Task HandleAsync(DeletePetCommand command, CancellationToken cancellationToken)
    {
        await validator.ValidateRequestAsync(command, cancellationToken);

        var pet = await pets.GetByIdAsync(command.PetId, cancellationToken)
            ?? throw new KeyNotFoundException($"Pet '{command.PetId}' was not found.");

        pets.Remove(pet);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
