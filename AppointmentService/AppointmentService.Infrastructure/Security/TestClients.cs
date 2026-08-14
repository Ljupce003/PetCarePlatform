namespace AppointmentService.Infrastructure.Security;

public sealed record TestClient(string ClientId, string ClientSecret);

/// <summary>
/// The client identity <see cref="LocalServiceAccessTokenProvider"/> issues a locally-signed
/// service-to-service token for. Not reachable over HTTP — purely an in-process stand-in for the
/// real <c>appointment-service</c> confidential client's Keycloak client-credentials grant, which
/// is what should replace this once Pet Service actually validates the bearer token it receives.
/// </summary>
public static class TestClients
{
    public static readonly TestClient AppointmentService = new("appointment-service", "appointment-secret");

    public static readonly IReadOnlyList<TestClient> All = [AppointmentService];

    public static TestClient? Find(string? clientId, string? clientSecret) =>
        All.FirstOrDefault(client => client.ClientId == clientId && client.ClientSecret == clientSecret);
}
