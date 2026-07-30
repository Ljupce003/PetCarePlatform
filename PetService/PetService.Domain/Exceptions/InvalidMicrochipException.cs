namespace PetService.Domain.Exceptions;

public class InvalidMicrochipException : Exception
{
    public InvalidMicrochipException(string microchipNumber)
        : base($"Microchip number '{microchipNumber}' must contain 8 to 30 letters or digits.")
    {
    }
}
