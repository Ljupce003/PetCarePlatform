using System.Security.Claims;

namespace AppointmentService.Api.Security;

/// <summary>Allows owners to operate only on resources linked to their Keycloak subject.</summary>
public static class UserOwnership
{
    public static bool CanAccessOwner(ClaimsPrincipal user, Guid ownerId) =>
        user.IsInRole("admin") ||
        (user.IsInRole("owner") &&
         Guid.TryParse(user.FindFirst("sub")?.Value, out var subjectId) &&
         subjectId == ownerId);

    public static bool CanAccessVeterinarian(ClaimsPrincipal user, Guid veterinarianId) =>
        user.IsInRole("admin") ||
        (user.IsInRole("veterinarian") &&
         Guid.TryParse(user.FindFirst("sub")?.Value, out var subjectId) &&
         subjectId == veterinarianId);
}
