using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PetService.Application.Commands;
using PetService.Application.Dtos;
using PetService.Application.Queries;
using PetService.Application.Requests;

namespace PetService.Api.Controllers;

[ApiController]
[Route("pets")]
[Authorize]
public class PetsController(
    RegisterPetHandler registerPet,
    UpdatePetHandler updatePet,
    DeletePetHandler deletePet,
    GetPetByIdHandler getPetById,
    GetAllPetsHandler getAllPets,
    CheckPetOwnershipHandler checkPetOwnership) : ControllerBase
{
    [HttpPost]
    [Authorize(Roles = "owner,admin")]
    [ProducesResponseType<PetDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<PetDto>> Register(
        CreatePetRequest request,
        CancellationToken cancellationToken)
    {
        var pet = await registerPet.HandleAsync(new RegisterPetCommand(request), cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = pet.PetId }, pet);
    }

    [HttpGet("{id:guid}")]
    [Authorize(Roles = "owner,admin")]
    [ProducesResponseType<PetDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PetDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var pet = await getPetById.HandleAsync(new GetPetByIdQuery(id), cancellationToken);
        return pet is null
            ? NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Pet not found",
                Detail = $"Pet '{id}' was not found."
            })
            : Ok(pet);
    }

    [HttpGet]
    [Authorize(Roles = "owner,admin")]
    [ProducesResponseType<IReadOnlyList<PetDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<PetDto>>> GetAll(CancellationToken cancellationToken) =>
        Ok(await getAllPets.HandleAsync(new GetAllPetsQuery(), cancellationToken));

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "owner,admin")]
    [ProducesResponseType<PetDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<PetDto>> Update(
        Guid id,
        UpdatePetRequest request,
        CancellationToken cancellationToken) =>
        Ok(await updatePet.HandleAsync(new UpdatePetCommand(id, request), cancellationToken));

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "owner,admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await deletePet.HandleAsync(new DeletePetCommand(id), cancellationToken);
        return NoContent();
    }

    [HttpGet("{id:guid}/exists")]
    [Authorize(Roles = "owner,admin,service")]
    [ProducesResponseType<PetOwnershipDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PetOwnershipDto>> CheckOwnership(
        Guid id,
        [FromQuery] Guid ownerId,
        CancellationToken cancellationToken) =>
        Ok(await checkPetOwnership.HandleAsync(
            new CheckPetOwnershipQuery(id, ownerId),
            cancellationToken));
}
