using System.Net;
using MCPServer.Contracts;

namespace MCPServer.Clients;

public sealed class PetServiceClient(HttpClient httpClient)
{
    public async Task<PetResponse?> GetPetAsync(Guid petId, CancellationToken cancellationToken)
    {
        using var response = await httpClient.GetAsync($"pets/{petId:D}", cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound)
            return null;

        await DownstreamResponse.EnsureSuccessAsync("Pet Service", response, cancellationToken);
        return await response.Content.ReadFromJsonAsync<PetResponse>(cancellationToken: cancellationToken)
            ?? throw new InvalidOperationException("Pet Service returned an empty response.");
    }

    public async Task<IReadOnlyList<PetResponse>> GetOwnerPetsAsync(
        Guid ownerId,
        CancellationToken cancellationToken)
    {
        var pets = await httpClient.GetFromJsonAsync<List<PetResponse>>(
            $"owners/{ownerId:D}/pets",
            cancellationToken);
        return pets ?? [];
    }
}
