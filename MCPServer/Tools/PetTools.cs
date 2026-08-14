using System.ComponentModel;
using MCPServer.Clients;
using MCPServer.Contracts;
using ModelContextProtocol.Server;

namespace MCPServer.Tools;

[McpServerToolType]
public sealed class PetTools(PetServiceClient petClient)
{
    [McpServerTool(Name = "get_pet")]
    [Description("Gets a pet by its unique identifier, or null when the pet does not exist.")]
    public Task<PetResponse?> GetPet(
        [Description("The unique pet identifier.")] Guid petId,
        CancellationToken cancellationToken) =>
        petClient.GetPetAsync(petId, cancellationToken);

    [McpServerTool(Name = "get_owner_pets")]
    [Description("Gets every pet registered to an owner.")]
    public Task<IReadOnlyList<PetResponse>> GetOwnerPets(
        [Description("The unique owner identifier.")] Guid ownerId,
        CancellationToken cancellationToken) =>
        petClient.GetOwnerPetsAsync(ownerId, cancellationToken);
}
