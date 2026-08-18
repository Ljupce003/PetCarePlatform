using System.Security.Claims;

namespace TreatmentAndNotificationService.API.Security;

public static class UserOwnership
{
    public static bool CanAccessOwner(ClaimsPrincipal user, Guid ownerId) =>
        user.IsInRole("admin") ||
        (user.IsInRole("owner") &&
         Guid.TryParse(user.FindFirst("sub")?.Value, out var subjectId) && subjectId == ownerId);

    public static bool CanAccessVeterinarian(ClaimsPrincipal user, Guid veterinarianId) =>
        user.IsInRole("admin") ||
        (user.IsInRole("veterinarian") &&
         Guid.TryParse(user.FindFirst("sub")?.Value, out var subjectId) && subjectId == veterinarianId);
}
