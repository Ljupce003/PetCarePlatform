namespace PetService.Domain.Exceptions;

public class InvalidBirthDateException : Exception
{
    public InvalidBirthDateException(DateOnly birthDate)
        : base($"Birth date '{birthDate:yyyy-MM-dd}' must be specified and cannot be in the future.")
    {
    }
}
