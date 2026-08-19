using FluentValidation;
using PetService.Application.Abstractions;
using PetService.Application.Dtos;
using PetService.Application.Requests;
using PetService.Application.Validation;
using PetService.Domain.Exceptions;

namespace PetService.Application.Commands;

public record RegisterPetCommand(CreatePetRequest Request);

public class RegisterPetCommandValidator : AbstractValidator<RegisterPetCommand>
{
    public RegisterPetCommandValidator(IValidator<CreatePetRequest> requestValidator)
    {
        RuleFor(command => command.Request).NotNull().SetValidator(requestValidator);
    }
}

public class RegisterPetHandler(
    IOwnerRepository owners,
    IPetRepository pets,
    IUnitOfWork unitOfWork,
    IValidator<RegisterPetCommand> validator)
{
    public async Task<PetDto> HandleAsync(RegisterPetCommand command, CancellationToken cancellationToken)
    {
        await validator.ValidateRequestAsync(command, cancellationToken);

        _ = await owners.GetByIdAsync(command.Request.OwnerId, cancellationToken)
            ?? throw new OwnerNotFoundException(command.Request.OwnerId);

        await EnsureMicrochipIsAvailableAsync(command.Request.MicrochipNumber, null, cancellationToken);

        var pet = command.Request.ToEntity();
        await pets.AddAsync(pet, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return pet.ToDto();
    }

    private async Task EnsureMicrochipIsAvailableAsync(
        string? microchipNumber,
        Guid? excludingPetId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(microchipNumber))
        {
            return;
        }

        var normalized = microchipNumber.Trim().ToUpperInvariant();
        if (await pets.ExistsWithMicrochipAsync(normalized, excludingPetId, cancellationToken))
        {
            throw new PetAlreadyExistsException(normalized);
        }
    }
}
