using FluentValidation;
using PetService.Application.Requests;

namespace PetService.Application.Validation;

public class CreatePetRequestValidator : AbstractValidator<CreatePetRequest>
{
    public CreatePetRequestValidator()
    {
        RuleFor(request => request.OwnerId).NotEmpty();
        PetValidationRules.Apply(this);
    }
}

public class UpdatePetRequestValidator : AbstractValidator<UpdatePetRequest>
{
    public UpdatePetRequestValidator()
    {
        PetValidationRules.Apply(this);
    }
}

internal static class PetValidationRules
{
    public static void Apply<T>(AbstractValidator<T> validator) where T : class
    {
        validator.RuleFor(request => GetName(request))
            .NotEmpty().WithName("Name")
            .MaximumLength(100).WithName("Name");

        validator.RuleFor(request => GetSpecies(request))
            .IsInEnum().WithName("Species");

        validator.RuleFor(request => GetBreed(request))
            .MaximumLength(100).WithName("Breed");

        validator.RuleFor(request => GetBirthDate(request))
            .Must(date => date != default && date <= DateOnly.FromDateTime(DateTime.UtcNow))
            .WithName("BirthDate")
            .WithMessage("BirthDate must be specified and cannot be in the future.");

        validator.RuleFor(request => GetWeight(request))
            .GreaterThan(0).WithName("Weight");

        validator.RuleFor(request => GetMicrochipNumber(request))
            .Matches("^[A-Za-z0-9]{8,30}$")
            .When(request => !string.IsNullOrWhiteSpace(GetMicrochipNumber(request)))
            .WithName("MicrochipNumber")
            .WithMessage("MicrochipNumber must contain 8 to 30 letters or digits.");

        validator.RuleForEach(request => GetAllergies(request))
            .NotEmpty().WithName("Allergies")
            .MaximumLength(200).WithName("Allergies");

        validator.RuleForEach(request => GetChronicConditions(request))
            .NotEmpty().WithName("ChronicConditions")
            .MaximumLength(200).WithName("ChronicConditions");
    }

    private static string GetName<T>(T request) => request switch
    {
        CreatePetRequest create => create.Name,
        UpdatePetRequest update => update.Name,
        _ => string.Empty
    };

    private static Domain.Enums.PetSpecies GetSpecies<T>(T request) => request switch
    {
        CreatePetRequest create => create.Species,
        UpdatePetRequest update => update.Species,
        _ => default
    };

    private static string? GetBreed<T>(T request) => request switch
    {
        CreatePetRequest create => create.Breed,
        UpdatePetRequest update => update.Breed,
        _ => null
    };

    private static DateOnly GetBirthDate<T>(T request) => request switch
    {
        CreatePetRequest create => create.BirthDate,
        UpdatePetRequest update => update.BirthDate,
        _ => default
    };

    private static decimal GetWeight<T>(T request) => request switch
    {
        CreatePetRequest create => create.Weight,
        UpdatePetRequest update => update.Weight,
        _ => default
    };

    private static string? GetMicrochipNumber<T>(T request) => request switch
    {
        CreatePetRequest create => create.MicrochipNumber,
        UpdatePetRequest update => update.MicrochipNumber,
        _ => null
    };

    private static IEnumerable<string> GetAllergies<T>(T request) => request switch
    {
        CreatePetRequest create => create.Allergies ?? [],
        UpdatePetRequest update => update.Allergies ?? [],
        _ => []
    };

    private static IEnumerable<string> GetChronicConditions<T>(T request) => request switch
    {
        CreatePetRequest create => create.ChronicConditions ?? [],
        UpdatePetRequest update => update.ChronicConditions ?? [],
        _ => []
    };
}
