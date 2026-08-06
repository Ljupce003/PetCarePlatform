using FluentValidation;
using PetService.Application.Abstractions;
using PetService.Application.Dtos;
using PetService.Application.Requests;
using PetService.Application.Validation;
using PetService.Domain.Exceptions;

namespace PetService.Application.Commands;

public record UpdatePetCommand(Guid PetId, UpdatePetRequest Request);

public class UpdatePetCommandValidator : AbstractValidator<UpdatePetCommand>
{
    public UpdatePetCommandValidator(IValidator<UpdatePetRequest> requestValidator)
    {
        RuleFor(command => command.PetId).NotEmpty();
        RuleFor(command => command.Request).NotNull().SetValidator(requestValidator);
    }
}

public class UpdatePetHandler(
    IPetRepository pets,
    IUnitOfWork unitOfWork,
    IValidator<UpdatePetCommand> validator)
{
    public async Task<PetDto> HandleAsync(UpdatePetCommand command, CancellationToken cancellationToken)
    {
        await validator.ValidateRequestAsync(command, cancellationToken);

        var pet = await pets.GetByIdAsync(command.PetId, cancellationToken)
            ?? throw new KeyNotFoundException($"Pet '{command.PetId}' was not found.");

        if (!string.IsNullOrWhiteSpace(command.Request.MicrochipNumber))
        {
            var normalized = command.Request.MicrochipNumber.Trim().ToUpperInvariant();
            if (await pets.ExistsWithMicrochipAsync(normalized, command.PetId, cancellationToken))
            {
                throw new PetAlreadyExistsException(normalized);
            }
        }

        command.Request.ApplyTo(pet);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return pet.ToDto();
    }
}
