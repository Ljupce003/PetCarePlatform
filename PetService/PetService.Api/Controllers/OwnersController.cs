using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PetService.Application.Commands;
using PetService.Application.Dtos;
using PetService.Application.Queries;
using PetService.Application.Requests;

namespace PetService.Api.Controllers;

[ApiController]
[Route("owners")]
[Authorize(Roles = "owner,admin")]
public class OwnersController(
    CreateOwnerHandler createOwner,
    UpdateOwnerHandler updateOwner,
    DeleteOwnerHandler deleteOwner,
    GetOwnerHandler getOwner,
    GetAllOwnersHandler getAllOwners,
    GetOwnerPetsHandler getOwnerPets) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType<OwnerDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<OwnerDto>> Create(
        CreateOwnerRequest request,
        CancellationToken cancellationToken)
    {
        var owner = await createOwner.HandleAsync(
            new CreateOwnerCommand(request.OwnerName, request.Email, request.Phone, request.Address),
            cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = owner.OwnerId }, owner);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType<OwnerDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OwnerDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var owner = await getOwner.HandleAsync(new GetOwnerQuery(id), cancellationToken);
        return owner is null
            ? NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Owner not found",
                Detail = $"Owner '{id}' was not found."
            })
            : Ok(owner);
    }

    [HttpGet]
    [ProducesResponseType<IReadOnlyList<OwnerDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<OwnerDto>>> GetAll(CancellationToken cancellationToken) =>
        Ok(await getAllOwners.HandleAsync(new GetAllOwnersQuery(), cancellationToken));

    [HttpPut("{id:guid}")]
    [ProducesResponseType<OwnerDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OwnerDto>> Update(
        Guid id,
        UpdateOwnerRequest request,
        CancellationToken cancellationToken) =>
        Ok(await updateOwner.HandleAsync(
            new UpdateOwnerCommand(id, request.OwnerName, request.Email, request.Phone, request.Address),
            cancellationToken));

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await deleteOwner.HandleAsync(new DeleteOwnerCommand(id), cancellationToken);
        return NoContent();
    }

    [HttpGet("{ownerId:guid}/pets")]
    [ProducesResponseType<IReadOnlyList<PetDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<PetDto>>> GetPets(
        Guid ownerId,
        CancellationToken cancellationToken) =>
        Ok(await getOwnerPets.HandleAsync(new GetOwnerPetsQuery(ownerId), cancellationToken));
}
