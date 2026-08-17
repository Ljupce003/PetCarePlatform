using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PetService.Application.Commands;
using PetService.Application.Dtos;
using PetService.Application.Queries;
using PetService.Application.Requests;

namespace PetService.Api.Controllers;

[ApiController]
[Route("pets")]
[Route("api/pets")]
[Authorize]
public class PetsController(
    RegisterPetHandler registerPet,
    UpdatePetHandler updatePet,
    DeletePetHandler deletePet,
    GetPetByIdHandler getPetById,
    GetAllPetsHandler getAllPets,
    CheckPetOwnershipHandler checkPetOwnership) : ControllerBase
{
    /// <summary>Registers a pet for an existing owner.</summary>
    /// <response code="201">The pet was registered.</response>
    /// <response code="400">The pet data is invalid.</response>
    /// <response code="404">The owner does not exist.</response>
    /// <response code="409">The microchip is already registered.</response>
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

    /// <summary>Gets a pet by identifier.</summary>
    /// <response code="200">The pet was found.</response>
    /// <response code="404">The pet does not exist.</response>
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

    /// <summary>Lists all pets ordered by name.</summary>
    [HttpGet]
    [Authorize(Roles = "owner,admin")]
    [ProducesResponseType<IReadOnlyList<PetDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<PetDto>>> GetAll(CancellationToken cancellationToken) =>
        Ok(await getAllPets.HandleAsync(new GetAllPetsQuery(), cancellationToken));

    /// <summary>Updates a pet's profile and medical metadata.</summary>
    /// <response code="200">The pet was updated.</response>
    /// <response code="400">The pet data is invalid.</response>
    /// <response code="404">The pet does not exist.</response>
    /// <response code="409">The microchip is already registered to another pet.</response>
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

    /// <summary>Deletes a pet.</summary>
    /// <response code="204">The pet was deleted.</response>
    /// <response code="404">The pet does not exist.</response>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "owner,admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await deletePet.HandleAsync(new DeletePetCommand(id), cancellationToken);
        return NoContent();
    }

    /// <summary>Verifies whether a pet exists and belongs to the requested owner.</summary>
    /// <remarks>
    /// This is the minimal anti-corruption contract consumed by Appointment Service. It is
    /// available under both <c>/pets</c> and the consumer's existing <c>/api/pets</c> prefix.
    /// </remarks>
    /// <response code="200">The pet exists; the response indicates whether ownership matches.</response>
    /// <response code="400">A pet or owner identifier is empty.</response>
    /// <response code="404">The pet does not exist.</response>
    [HttpGet("{id:guid}/exists")]
    [Authorize(Roles = "owner,admin,service")]
    [ProducesResponseType<PetOwnershipDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PetOwnershipDto>> CheckOwnership(
        Guid id,
        [FromQuery] Guid ownerId,
        CancellationToken cancellationToken)
    {
        var ownership = await checkPetOwnership.HandleAsync(
            new CheckPetOwnershipQuery(id, ownerId),
            cancellationToken);

        return ownership.Exists ? Ok(ownership) : NotFound();
    }
}
