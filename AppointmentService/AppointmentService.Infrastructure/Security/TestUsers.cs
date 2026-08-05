using AppointmentService.Infrastructure.Persistence;

namespace AppointmentService.Infrastructure.Security;

public sealed record TestUser(Guid Id, string Username, string Password, string Role);

/// <summary>
/// One fixed demo user per role required by the task list (owner, veterinarian, admin), for
/// logging in via <c>POST /auth/login</c>. Dev/test only — plaintext passwords are fine here
/// because this whole login endpoint is a stand-in for Keycloak (see README), not a real identity
/// store, and none of this ships as a real user database.
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
/// Registered "client applications" for the OAuth2 client-credentials grant at
/// <c>POST /auth/token</c> — this service is the only one registered today, since it's the only
/// caller of <see cref="LocalServiceAccessTokenProvider"/>.
/// </summary>
public static class TestClients
{
    public static readonly TestClient AppointmentService = new("appointment-service", "appointment-secret");

    public static readonly IReadOnlyList<TestClient> All = [AppointmentService];

    public static TestClient? Find(string? clientId, string? clientSecret) =>
        All.FirstOrDefault(client => client.ClientId == clientId && client.ClientSecret == clientSecret);
}
