using Microsoft.Extensions.DependencyInjection;

namespace PetService.Application;

public static class DependencyInjection
{
    /// <summary>
    /// Registers the application layer. Use cases and request validators are added here
    /// as they are implemented; the layer already owns its registration so the API never
    /// has to know what lives inside it.
    /// </summary>
    public static IServiceCollection AddPetServiceApplication(this IServiceCollection services) => services;
}
