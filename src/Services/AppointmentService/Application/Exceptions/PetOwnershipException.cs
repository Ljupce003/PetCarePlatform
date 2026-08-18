namespace AppointmentService.Application.Exceptions;

/// <summary>
/// Thrown when the Pet Service confirms the pet exists but says it isn't owned by the owner on
/// the booking request. Kept distinct from "not found" (that's a plain
/// <see cref="KeyNotFoundException"/>), since the two should map to different HTTP responses
/// once the API layer exists.
/// </summary>
public sealed class PetOwnershipException : Exception
{
    public PetOwnershipException(Guid petId, Guid ownerId)
        : base($"Pet '{petId}' is not owned by owner '{ownerId}'.")
    {
        PetId = petId;
        OwnerId = ownerId;
    }

    public Guid PetId { get; }
    public Guid OwnerId { get; }
}
