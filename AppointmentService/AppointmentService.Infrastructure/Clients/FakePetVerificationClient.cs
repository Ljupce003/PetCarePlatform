using AppointmentService.Application.Abstractions;

namespace AppointmentService.Infrastructure.Clients;

/// <summary>
/// Isolated-development and test stand-in for <see cref="PetServiceClient"/>. Pet Service already
/// exposes the real ownership contract; this implementation is used only when that service is
/// intentionally absent.
/// </summary>
/// <remarks>
/// Enabled via <c>PetService:UseFakeVerification = true</c> (see
/// <see cref="DependencyInjection.AddPetServiceClient"/>) — on by default in
/// appsettings.Development.json and overridable per environment. Treats any non-empty
/// petId/ownerId as a valid, owned pet; only
/// <see cref="Guid.Empty"/> is rejected, so command validation errors are still reachable.
/// Keep the flag disabled when verifying the real cross-service integration.
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
