using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.IdentityModel.Tokens;

namespace MCPServer.IntegrationTests;

public sealed class McpProtocolTests(McpServerFactory factory) : IClassFixture<McpServerFactory>
{
    private const string Issuer = "appointment-service";
    private const string Audience = "petcare";
    private const string SigningKey = "dev-only-signing-key-change-me-32-chars-minimum!!";
    private static readonly Guid PetId = Guid.Parse("a1111111-1111-1111-1111-111111111111");

    [Fact]
    public async Task Health_IsAvailableWithoutAuthentication()
    {
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task McpEndpoint_WithoutToken_ReturnsUnauthorized()
    {
        using var client = factory.CreateClient();
        using var request = CreateMcpRequest(InitializePayload());

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Initialize_WithValidToken_ReturnsServerCapabilities()
    {
        using var client = factory.CreateClient();
        using var request = CreateMcpRequest(InitializePayload(), CreateToken("owner"));

        using var response = await client.SendAsync(request);
        var payload = await ReadSseDataAsync(response);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("PetCare MCP Server", payload.RootElement.GetProperty("result").GetProperty("serverInfo").GetProperty("name").GetString());
        Assert.True(payload.RootElement.GetProperty("result").GetProperty("capabilities").TryGetProperty("tools", out _));
    }

    [Fact]
    public async Task ToolsList_ContainsAllTreatmentToolsWithDescriptions()
    {
        using var client = factory.CreateClient();
        using var request = CreateMcpRequest(
            """
            {"jsonrpc":"2.0","id":2,"method":"tools/list","params":{}}
            """,
            CreateToken("owner"),
            includeProtocolVersion: true);

        using var response = await client.SendAsync(request);
        var payload = await ReadSseDataAsync(response);
        var tools = payload.RootElement.GetProperty("result").GetProperty("tools").EnumerateArray().ToArray();
        var names = tools.Select(tool => tool.GetProperty("name").GetString()).ToArray();

        Assert.Equal(13, tools.Length);
        Assert.Contains("get_pet", names);
        Assert.Contains("get_owner_pets", names);
        Assert.Contains("find_available_veterinarians", names);
        Assert.Contains("get_upcoming_appointments", names);
        Assert.Contains("search_clinics", names);
        Assert.Contains("search_available_slots", names);
        Assert.Contains("find_open_appointment_slots", names);
        Assert.Contains("create_available_slot", names);
        Assert.Contains("get_medical_history", names);
        Assert.Contains("get_vaccination_history", names);
        Assert.Contains("get_next_vaccination", names);
        Assert.Contains("record_medical_examination", names);
        Assert.Contains("record_vaccination", names);
        Assert.All(tools, tool => Assert.False(string.IsNullOrWhiteSpace(tool.GetProperty("description").GetString())));
    }

    [Fact]
    public async Task GetMedicalHistory_CallsTreatmentApiAndForwardsBearerToken()
    {
        using var client = factory.CreateClient();
        var token = CreateToken("owner");
        var callPayload = JsonSerializer.Serialize(new
        {
            jsonrpc = "2.0",
            id = 3,
            method = "tools/call",
            @params = new
            {
                name = "get_medical_history",
                arguments = new { petId = PetId }
            }
        });
        using var request = CreateMcpRequest(
            callPayload,
            token,
            includeProtocolVersion: true);

        using var response = await client.SendAsync(request);
        var payload = await ReadSseDataAsync(response);
        var resultText = payload.RootElement
            .GetProperty("result")
            .GetProperty("content")[0]
            .GetProperty("text")
            .GetString();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa", resultText);
        Assert.Equal($"/api/treatments/pet/{PetId:D}", factory.TreatmentHandler.LastPath);
        Assert.Equal($"Bearer {token}", factory.TreatmentHandler.LastAuthorization);
    }

    [Fact]
    public async Task GetPet_ReturnsPetServiceResponse()
    {
        using var client = factory.CreateClient();
        var payload = await CallToolAsync(
            client,
            "get_pet",
            new { petId = PetId },
            CreateToken("owner"));
        var text = ToolText(payload);

        Assert.Contains("Milo", text);
        Assert.Contains("Labrador", text);
        Assert.Equal($"/pets/{PetId:D}", factory.PetHandler.LastPath);
    }

    [Fact]
    public async Task GetOwnerPets_ReturnsPetCollection()
    {
        using var client = factory.CreateClient();
        var ownerId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var payload = await CallToolAsync(
            client,
            "get_owner_pets",
            new { ownerId },
            CreateToken("owner"));

        Assert.Contains("Milo", ToolText(payload));
        Assert.Equal($"/owners/{ownerId:D}/pets", factory.PetHandler.LastPath);
    }

    [Fact]
    public async Task FindAvailableVeterinarians_AppliesFiltersAndRemovesUnavailableResults()
    {
        using var client = factory.CreateClient();
        var clinicId = Guid.Parse("33333333-3333-3333-3333-333333333333");
        var token = CreateToken("owner");
        var payload = await CallToolAsync(
            client,
            "find_available_veterinarians",
            new { clinicId, specialization = "Surgery" },
            token);
        var text = ToolText(payload);

        Assert.Contains("Dr. Ana", text);
        Assert.DoesNotContain("Dr. Mark", text);
        Assert.Equal($"/veterinarians?clinicId={clinicId:D}&specialization=Surgery", factory.AppointmentHandler.LastPath);
        Assert.Equal($"Bearer {token}", factory.AppointmentHandler.LastAuthorization);
    }

    [Fact]
    public async Task GetUpcomingAppointments_ReturnsAppointmentServiceResponse()
    {
        using var client = factory.CreateClient();
        var ownerId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var payload = await CallToolAsync(
            client,
            "get_upcoming_appointments",
            new { ownerId },
            CreateToken("owner"));

        Assert.Contains("Annual checkup", ToolText(payload));
        Assert.Equal($"/appointments/upcoming?ownerId={ownerId:D}", factory.AppointmentHandler.LastPath);
    }

    [Fact]
    public async Task SearchClinics_ReturnsAppointmentServiceResponse()
    {
        using var client = factory.CreateClient();
        var payload = await CallToolAsync(
            client,
            "search_clinics",
            new { location = "Skopje" },
            CreateToken("owner"));

        Assert.Contains("Central Vet Clinic", ToolText(payload));
        Assert.Equal("/clinics?location=Skopje", factory.AppointmentHandler.LastPath);
    }

    [Fact]
    public async Task SearchAvailableSlots_ReturnsAppointmentServiceResponse()
    {
        using var client = factory.CreateClient();
        var veterinarianId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var payload = await CallToolAsync(
            client,
            "search_available_slots",
            new { veterinarianId },
            CreateToken("owner"));

        Assert.Contains("Dr. Ana", ToolText(payload));
        Assert.Equal($"/slots?veterinarianId={veterinarianId:D}", factory.AppointmentHandler.LastPath);
    }

    [Fact]
    public async Task FindOpenAppointmentSlots_ReturnsGroupedVeterinariansWithSlots()
    {
        using var client = factory.CreateClient();
        var payload = await CallToolAsync(
            client,
            "find_open_appointment_slots",
            new { date = "2026-08-18", location = "Skopje" },
            CreateToken("owner"));
        var text = ToolText(payload);

        Assert.Contains("Dr. Ana", text);
        Assert.Contains("availableSlots", text);
        Assert.Equal("/veterinarians/available?date=2026-08-18&location=Skopje", factory.AppointmentHandler.LastPath);
    }

    [Fact]
    public async Task CreateAvailableSlot_PostsToAppointmentServiceAndForwardsBearerToken()
    {
        using var client = factory.CreateClient();
        var veterinarianId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var token = CreateToken("admin");
        var payload = await CallToolAsync(
            client,
            "create_available_slot",
            new
            {
                veterinarianId,
                startsAtUtc = "2026-08-18T14:00:00Z",
                endsAtUtc = "2026-08-18T14:30:00Z"
            },
            token);

        Assert.Contains("Dr. Ana", ToolText(payload));
        Assert.Equal("/slots", factory.AppointmentHandler.LastPath);
        Assert.Equal($"Bearer {token}", factory.AppointmentHandler.LastAuthorization);
    }

    private static HttpRequestMessage CreateMcpRequest(
        string json,
        string? token = null,
        bool includeProtocolVersion = false)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/mcp")
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/event-stream"));

        if (token is not null)
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        if (includeProtocolVersion)
            request.Headers.TryAddWithoutValidation("MCP-Protocol-Version", "2025-11-25");

        return request;
    }

    private static string InitializePayload() =>
        """
        {"jsonrpc":"2.0","id":1,"method":"initialize","params":{"protocolVersion":"2025-11-25","capabilities":{},"clientInfo":{"name":"integration-tests","version":"1.0"}}}
        """;

    private static async Task<JsonDocument> ReadSseDataAsync(HttpResponseMessage response)
    {
        var body = await response.Content.ReadAsStringAsync();
        var dataLine = body.Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Single(line => line.StartsWith("data: ", StringComparison.Ordinal));
        return JsonDocument.Parse(dataLine[6..]);
    }

    private static async Task<JsonDocument> CallToolAsync(
        HttpClient client,
        string name,
        object arguments,
        string token)
    {
        var json = JsonSerializer.Serialize(new
        {
            jsonrpc = "2.0",
            id = Guid.NewGuid().ToString("N"),
            method = "tools/call",
            @params = new { name, arguments }
        });
        using var request = CreateMcpRequest(json, token, includeProtocolVersion: true);
        using var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await ReadSseDataAsync(response);
    }

    private static string ToolText(JsonDocument payload) =>
        payload.RootElement.GetProperty("result").GetProperty("content")[0].GetProperty("text").GetString()
        ?? string.Empty;

    private static string CreateToken(string role)
    {
        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(SigningKey)),
            SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: Issuer,
            audience: Audience,
            claims:
            [
                new Claim(JwtRegisteredClaimNames.Sub, $"{role}-mcp-test"),
                new Claim(ClaimTypes.Role, role)
            ],
            expires: DateTime.UtcNow.AddMinutes(10),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
