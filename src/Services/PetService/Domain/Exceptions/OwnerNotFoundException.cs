namespace PetService.Domain.Exceptions;

public class OwnerNotFoundException : Exception
{
    public OwnerNotFoundException(Guid ownerId)
        : base($"Owner '{ownerId}' was not found.")
    {
        OwnerId = ownerId;
    }

    public Guid OwnerId { get; }
}
