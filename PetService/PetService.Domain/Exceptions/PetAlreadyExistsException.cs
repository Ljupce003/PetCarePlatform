namespace PetService.Domain.Exceptions;

public class PetAlreadyExistsException : Exception
{
    public PetAlreadyExistsException(string microchipNumber)
        : base($"A pet with microchip number '{microchipNumber}' already exists.")
    {
        MicrochipNumber = microchipNumber;
    }

    public string MicrochipNumber { get; }
}
