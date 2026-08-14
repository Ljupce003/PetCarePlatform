using AppointmentService.Infrastructure.Persistence;

namespace AppointmentService.Infrastructure.Security;

public sealed record TestUser(Guid Id, string Username, string Password, string Role);

/// <summary>
/// One fixed demo user per role (owner, veterinarian, admin), consulted ONLY by
/// <c>AuthController.Login</c>'s "Testing" environment branch -- i.e. only inside
/// <c>AppointmentService.Api.IntegrationTests</c> (CI has no live Keycloak to log in against).
/// Every other environment (local <c>dotnet run</c>, Docker, production) always logs in against
/// the real Keycloak realm instead; see KeycloakAuthClient. The ids intentionally match the same
/// demo users seeded in infrastructure/keycloak/petcare-realm.json, so a token from either path
/// resolves to the same seeded owner/veterinarian this service already knows about.
/// </summary>
public static class TestUsers
{
    // Reuses AppointmentDbInitializer's demo ids where one already exists for that role, so
    // logging in as "owner1" gives you a token whose sub matches DemoOwnerId -- the same id
    // already used throughout the seeded data and the .http file.
    public static readonly TestUser Owner = new(AppointmentDbInitializer.DemoOwnerId, "owner1", "Owner123!", "owner");
    public static readonly TestUser Veterinarian = new(AppointmentDbInitializer.DemoVeterinarianId, "vet1", "Vet123!", "veterinarian");
    public static readonly TestUser Admin = new(new Guid("55555555-5555-5555-5555-555555555553"), "admin1", "Admin123!", "admin");

    public static readonly IReadOnlyList<TestUser> All = [Owner, Veterinarian, Admin];

    public static TestUser? Find(string? username, string? password) =>
        All.FirstOrDefault(user =>
            string.Equals(user.Username, username, StringComparison.OrdinalIgnoreCase) &&
            user.Password == password);
}
