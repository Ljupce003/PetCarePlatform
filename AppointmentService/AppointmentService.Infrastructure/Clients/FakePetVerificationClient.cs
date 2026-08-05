using AppointmentService.Application.Abstractions;

namespace AppointmentService.Infrastructure.Clients;

/// <summary>
/// Stand-in for <see cref="PetServiceClient"/> while Pet Service doesn't yet expose
/// <c>GET /api/pets/{id}/exists</c> (or have any seeded pets/owners to check against). Lets the
/// full booking workflow be tested end-to-end through this service's own Swagger UI without
/// depending on another team member's unfinished work.
/// </summary>
/// <remarks>
/// Enabled via <c>PetService:UseFakeVerification = true</c> (see
/// <see cref="DependencyInjection.AddPetServiceClient"/>) — on by default in
/// appsettings.Development.json, off everywhere else, so Docker/production always talk to the
/// real Pet Service. Treats any non-empty petId/ownerId as a valid, owned pet; only
/// <see cref="Guid.Empty"/> is rejected, so command validation errors are still reachable.
/// Swap the flag off (or delete this class) once Pet Service's real endpoint and seed data exist.
/// </remarks>
public sealed class FakePetVerificationClient : IPetVerificationClient
{
    public Task<PetVerificationResult> VerifyAsync(Guid petId, Guid ownerId, CancellationToken cancellationToken)
    {
        var result = petId != Guid.Empty && ownerId != Guid.Empty
            ? new PetVerificationResult(Exists: true, IsOwnedByOwner: true)
            : new PetVerificationResult(Exists: false, IsOwnedByOwner: false);

        return Task.FromResult(result);
    }
}
