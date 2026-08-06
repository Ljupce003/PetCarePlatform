using System.Net;
using System.Net.Http.Json;
using AppointmentService.Application.Abstractions;

namespace AppointmentService.Infrastructure.Clients;

/// <summary>
/// HttpClient-based implementation of <see cref="IPetVerificationClient"/>. Talks to the Pet
/// Service's anti-corruption endpoint (<c>GET /api/pets/{id}/exists?ownerId=...</c>, per the
/// Pet Service's own task list) and translates its response into the simplified
/// <see cref="PetVerificationResult"/> the Application layer expects.
/// </summary>
/// <remarks>
/// Retries for transient failures (timeouts, 5xx, connection resets) are configured on the
/// HttpClient itself via <c>AddStandardResilienceHandler()</c> in
/// <see cref="DependencyInjection.AddAppointmentServiceInfrastructure"/> — this class only
/// needs to worry about the happy path and Pet Service's own "not found" response.
/// </remarks>
public sealed class PetServiceClient(HttpClient httpClient) : IPetVerificationClient
{
    public async Task<PetVerificationResult> VerifyAsync(Guid petId, Guid ownerId, CancellationToken cancellationToken)
    {
        using var response = await httpClient.GetAsync($"/api/pets/{petId}/exists?ownerId={ownerId}", cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return new PetVerificationResult(Exists: false, IsOwnedByOwner: false);
        }

        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadFromJsonAsync<PetExistsResponse>(cancellationToken)
            ?? throw new InvalidOperationException("Pet Service returned an empty response body.");

        return new PetVerificationResult(payload.Exists, payload.OwnedByOwner);
    }

    // The Pet Service's own response shape. Kept private and separate from
    // PetVerificationResult on purpose — this is the anti-corruption boundary: if Pet Service
    // renames or reshapes this contract, only this record (and the mapping above) changes.
    private sealed record PetExistsResponse(bool Exists, bool OwnedByOwner);
}
