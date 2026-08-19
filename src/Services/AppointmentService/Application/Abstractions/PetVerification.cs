namespace AppointmentService.Application.Abstractions;

/// <summary>
/// Anti-corruption layer for the Pet Service: whatever shape their API actually returns stays
/// behind <see cref="IPetVerificationClient"/>'s implementation — the rest of the Appointment
/// Service only ever sees this simplified, stable result.
/// </summary>
public sealed record PetVerificationResult(bool Exists, bool IsOwnedByOwner);

public interface IPetVerificationClient
{
    Task<PetVerificationResult> VerifyAsync(Guid petId, Guid ownerId, CancellationToken cancellationToken);
}
