using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Configuration;

namespace AppointmentService.Infrastructure.Security;

public sealed class KeycloakAdminClient(HttpClient client, IConfiguration configuration)
{
    public async Task<Guid> CreateVeterinarianAsync(
        string username,
        string temporaryPassword,
        string fullName,
        CancellationToken cancellationToken)
    {
        var realm = configuration["KeycloakAdmin:Realm"] ?? "petcare";
        var token = await GetAdminTokenAsync(cancellationToken);
        var requestedId = Guid.NewGuid();
        using var createRequest = Authorized(HttpMethod.Post, $"admin/realms/{realm}/users", token);
        createRequest.Content = JsonContent.Create(new
        {
            id = requestedId,
            username = username.Trim(),
            enabled = true,
            firstName = fullName.Trim(),
            credentials = new[] { new { type = "password", value = temporaryPassword, temporary = true } }
        });
        using var createResponse = await client.SendAsync(createRequest, cancellationToken);
        if (createResponse.StatusCode == HttpStatusCode.Conflict)
            throw new InvalidOperationException($"Keycloak username '{username}' already exists.");
        createResponse.EnsureSuccessStatusCode();

        var accountIdText = createResponse.Headers.Location?.Segments.LastOrDefault()?.Trim('/');
        var accountId = Guid.TryParse(accountIdText, out var createdId) ? createdId : requestedId;
        try
        {
            using var roleRequest = Authorized(HttpMethod.Get, $"admin/realms/{realm}/roles/veterinarian", token);
            using var roleResponse = await client.SendAsync(roleRequest, cancellationToken);
            roleResponse.EnsureSuccessStatusCode();
            var role = await roleResponse.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: cancellationToken);

            using var mappingRequest = Authorized(HttpMethod.Post, $"admin/realms/{realm}/users/{accountId}/role-mappings/realm", token);
            mappingRequest.Content = JsonContent.Create(new[] { role });
            using var mappingResponse = await client.SendAsync(mappingRequest, cancellationToken);
            mappingResponse.EnsureSuccessStatusCode();
            return accountId;
        }
        catch
        {
            await DeleteUserAsync(accountId, token, cancellationToken);
            throw;
        }
    }

    public async Task DeleteVeterinarianAsync(Guid accountId, CancellationToken cancellationToken)
    {
        var token = await GetAdminTokenAsync(cancellationToken);
        await DeleteUserAsync(accountId, token, cancellationToken);
    }

    public async Task UpdateVeterinarianAsync(Guid accountId, string fullName, CancellationToken cancellationToken)
    {
        var realm = configuration["KeycloakAdmin:Realm"] ?? "petcare";
        var token = await GetAdminTokenAsync(cancellationToken);
        using var request = Authorized(HttpMethod.Put, $"admin/realms/{realm}/users/{accountId}", token);
        request.Content = JsonContent.Create(new { firstName = fullName.Trim() });
        using var response = await client.SendAsync(request, cancellationToken);
        if (response.StatusCode != HttpStatusCode.NotFound) response.EnsureSuccessStatusCode();
    }

    private async Task<string> GetAdminTokenAsync(CancellationToken cancellationToken)
    {
        var username = configuration["KeycloakAdmin:Username"]
            ?? throw new InvalidOperationException("KeycloakAdmin:Username is required.");
        var password = configuration["KeycloakAdmin:Password"]
            ?? throw new InvalidOperationException("KeycloakAdmin:Password is required.");
        using var response = await client.PostAsync(
            "realms/master/protocol/openid-connect/token",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "password",
                ["client_id"] = "admin-cli",
                ["username"] = username,
                ["password"] = password
            }), cancellationToken);
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: cancellationToken);
        return json.GetProperty("access_token").GetString()
            ?? throw new InvalidOperationException("Keycloak did not return an admin access token.");
    }

    private async Task DeleteUserAsync(Guid accountId, string token, CancellationToken cancellationToken)
    {
        var realm = configuration["KeycloakAdmin:Realm"] ?? "petcare";
        using var request = Authorized(HttpMethod.Delete, $"admin/realms/{realm}/users/{accountId}", token);
        using var response = await client.SendAsync(request, cancellationToken);
        if (response.StatusCode != HttpStatusCode.NotFound) response.EnsureSuccessStatusCode();
    }

    private static HttpRequestMessage Authorized(HttpMethod method, string path, string token)
    {
        var request = new HttpRequestMessage(method, path);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return request;
    }
}
