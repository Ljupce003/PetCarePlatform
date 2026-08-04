using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using PetService.Application.Commands;
using PetService.Application.Queries;
using PetService.Application.Requests;
using PetService.Application.Validation;

namespace PetService.Application;

public static class DependencyInjection
{
    /// <summary>
    /// Registers every Pet application use case and validator. Repositories and the unit of
    /// work are implemented by Infrastructure and bound there during service composition.
    /// </summary>
    public static IServiceCollection AddPetServiceApplication(this IServiceCollection services)
    {
        services.AddScoped<CreateOwnerHandler>();
        services.AddScoped<UpdateOwnerHandler>();
        services.AddScoped<DeleteOwnerHandler>();
        services.AddScoped<RegisterPetHandler>();
        services.AddScoped<UpdatePetHandler>();
        services.AddScoped<DeletePetHandler>();

        services.AddScoped<GetPetByIdHandler>();
        services.AddScoped<GetAllPetsHandler>();
        services.AddScoped<GetOwnerHandler>();
        services.AddScoped<GetOwnerPetsHandler>();
        services.AddScoped<CheckPetOwnershipHandler>();

        services.AddScoped<IValidator<CreatePetRequest>, CreatePetRequestValidator>();
        services.AddScoped<IValidator<UpdatePetRequest>, UpdatePetRequestValidator>();
        services.AddScoped<IValidator<CreateOwnerCommand>, CreateOwnerCommandValidator>();
        services.AddScoped<IValidator<UpdateOwnerCommand>, UpdateOwnerCommandValidator>();
        services.AddScoped<IValidator<DeleteOwnerCommand>, DeleteOwnerCommandValidator>();
        services.AddScoped<IValidator<RegisterPetCommand>, RegisterPetCommandValidator>();
        services.AddScoped<IValidator<UpdatePetCommand>, UpdatePetCommandValidator>();
        services.AddScoped<IValidator<DeletePetCommand>, DeletePetCommandValidator>();
        services.AddScoped<IValidator<GetPetByIdQuery>, GetPetByIdQueryValidator>();
        services.AddScoped<IValidator<GetOwnerQuery>, GetOwnerQueryValidator>();
        services.AddScoped<IValidator<GetOwnerPetsQuery>, GetOwnerPetsQueryValidator>();
        services.AddScoped<IValidator<CheckPetOwnershipQuery>, CheckPetOwnershipQueryValidator>();

        return services;
    }
}
