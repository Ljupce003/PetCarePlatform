using AppointmentService.Infrastructure.Persistence;

namespace AppointmentService.Infrastructure.Security;

public sealed record TestUser(Guid Id, string Username, string Password, string Role);

/// <summary>
/// One fixed demo user per role (owner, veterinarian, admin), used by <c>POST /auth/login</c> only
/// when running outside Docker without a real Keycloak to check against (see AuthController). The
/// ids intentionally match the same demo users seeded in infrastructure/keycloak/petcare-realm.json,
/// so a real Keycloak-issued token's <c>sub</c> claim resolves to the same seeded owner/veterinarian
/// this service already knows about.
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

public sealed record TestClient(string ClientId, string ClientSecret);

/// <summary>
/// The client identity <see cref="LocalServiceAccessTokenProvider"/> issues a locally-signed
/// service-to-service token for outside Docker. Not reachable over HTTP (no
/// <c>/auth/token</c> endpoint anymore) — purely an in-process stand-in for the real
/// <c>appointment-service</c> confidential client's Keycloak client-credentials grant, which is
/// what should replace this once Pet Service actually validates the bearer token it receives.
/// </summary>
public static class TestClients
{
    public static readonly TestClient AppointmentService = new("appointment-service", "appointment-secret");

    public static readonly IReadOnlyList<TestClient> All = [AppointmentService];

    public static TestClient? Find(string? clientId, string? clientSecret) =>
        All.FirstOrDefault(client => client.ClientId == clientId && client.ClientSecret == clientSecret);
}
