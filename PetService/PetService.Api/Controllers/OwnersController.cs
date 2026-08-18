using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PetService.Application.Commands;
using PetService.Application.Dtos;
using PetService.Application.Queries;
using PetService.Application.Requests;
using PetService.Api.Security;

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
    /// <summary>Creates a pet owner after validating contact information.</summary>
    /// <response code="201">The owner was created.</response>
    /// <response code="400">The owner data is invalid.</response>
    [HttpPost]
    [Authorize(Roles = "admin")]
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

    /// <summary>Gets an owner by identifier.</summary>
    /// <response code="200">The owner was found.</response>
    /// <response code="404">The owner does not exist.</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType<OwnerDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OwnerDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        if (!UserOwnership.CanAccessOwner(User, id)) return Forbid();
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

    /// <summary>Lists all owners ordered by name.</summary>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<OwnerDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<OwnerDto>>> GetAll(CancellationToken cancellationToken)
    {
        if (User.IsInRole("admin"))
            return Ok(await getAllOwners.HandleAsync(new GetAllOwnersQuery(), cancellationToken));

        if (!Guid.TryParse(User.FindFirst("sub")?.Value, out var ownerId)) return Forbid();
        var owner = await getOwner.HandleAsync(new GetOwnerQuery(ownerId), cancellationToken);
        return owner is null ? Ok(Array.Empty<OwnerDto>()) : Ok(new[] { owner });
    }

    /// <summary>Updates an owner's contact information.</summary>
    /// <response code="200">The owner was updated.</response>
    /// <response code="400">The owner data is invalid.</response>
    /// <response code="404">The owner does not exist.</response>
    [HttpPut("{id:guid}")]
    [ProducesResponseType<OwnerDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OwnerDto>> Update(
        Guid id,
        UpdateOwnerRequest request,
        CancellationToken cancellationToken)
    {
        if (!UserOwnership.CanAccessOwner(User, id)) return Forbid();
        return Ok(await updateOwner.HandleAsync(
            new UpdateOwnerCommand(id, request.OwnerName, request.Email, request.Phone, request.Address),
            cancellationToken));
    }

    /// <summary>Deletes an owner and their pets.</summary>
    /// <response code="204">The owner was deleted.</response>
    /// <response code="404">The owner does not exist.</response>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await deleteOwner.HandleAsync(new DeleteOwnerCommand(id), cancellationToken);
        return NoContent();
    }

    /// <summary>Lists every pet registered to an owner.</summary>
    /// <response code="200">The owner's pets were returned.</response>
    /// <response code="404">The owner does not exist.</response>
    [HttpGet("{ownerId:guid}/pets")]
    [ProducesResponseType<IReadOnlyList<PetDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<PetDto>>> GetPets(
        Guid ownerId,
        CancellationToken cancellationToken)
    {
        if (!UserOwnership.CanAccessOwner(User, ownerId)) return Forbid();
        return Ok(await getOwnerPets.HandleAsync(new GetOwnerPetsQuery(ownerId), cancellationToken));
    }
}
