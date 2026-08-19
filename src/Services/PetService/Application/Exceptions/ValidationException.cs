using FluentValidation.Results;

namespace PetService.Application.Exceptions;

/// <summary>
/// A stable application exception that keeps FluentValidation details out of API consumers.
/// </summary>
public class ValidationException : Exception
{
    public ValidationException(IEnumerable<ValidationFailure> failures)
        : this(failures
            .GroupBy(failure => failure.PropertyName)
            .ToDictionary(
                group => group.Key,
                group => group.Select(failure => failure.ErrorMessage).Distinct().ToArray()))
    {
    }

    private ValidationException(IReadOnlyDictionary<string, string[]> errors)
        : base("One or more validation errors occurred.")
    {
        Errors = errors;
    }

    public IReadOnlyDictionary<string, string[]> Errors { get; }
}
