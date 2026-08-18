using System.Security.Claims;

namespace PetService.Api.Security;

/// <summary>Maps the Keycloak subject claim to the matching PetCare owner identifier.</summary>
public static class UserOwnership
{
    public static bool CanAccessOwner(ClaimsPrincipal user, Guid ownerId) =>
        user.IsInRole("admin") ||
        (user.IsInRole("owner") &&
         Guid.TryParse(user.FindFirst("sub")?.Value, out var subjectId) &&
         subjectId == ownerId);
}
