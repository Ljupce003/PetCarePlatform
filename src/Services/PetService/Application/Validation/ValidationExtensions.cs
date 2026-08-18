using FluentValidation;

namespace PetService.Application.Validation;

internal static class ValidationExtensions
{
    public static async Task ValidateRequestAsync<T>(
        this IValidator<T> validator,
        T instance,
        CancellationToken cancellationToken)
    {
        var result = await validator.ValidateAsync(instance, cancellationToken);
        if (!result.IsValid)
        {
            throw new Exceptions.ValidationException(result.Errors);
        }
    }
}
