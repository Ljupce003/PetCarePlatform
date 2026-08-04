namespace AppointmentService.Application.Exceptions;

/// <summary>
/// Thrown when a command or query fails input validation before it ever reaches the domain
/// layer. Kept distinct from domain exceptions, which signal a violated business rule rather
/// than a malformed request.
/// </summary>
public sealed class ValidationException : Exception
{
    public ValidationException(IReadOnlyList<string> errors)
        : base("Validation failed: " + string.Join("; ", errors))
    {
        Errors = errors;
    }

    public IReadOnlyList<string> Errors { get; }
}
