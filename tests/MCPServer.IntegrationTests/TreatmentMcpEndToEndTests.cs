extern alias TreatmentService;

using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using MCPServer.Clients;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using Testcontainers.PostgreSql;
using TreatmentDbContext = TreatmentService::TreatmentAndNotificationService.Infrastructure.Persistence.TreatmentDbContext;
using TreatmentProgram = TreatmentService::Program;

namespace MCPServer.IntegrationTests;

public sealed class TreatmentMcpEndToEndFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _database = new PostgreSqlBuilder("postgres:17-alpine")
        .WithDatabase("mcp_treatment_tests")
        .WithUsername("mcp_tests")
        .WithPassword("mcp_tests")
        .Build();
    private RealTreatmentFactory? _treatmentFactory;

    public McpWithRealTreatmentFactory McpFactory { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        await _database.StartAsync();
        _treatmentFactory = new RealTreatmentFactory(_database.GetConnectionString());

        // Starting a client boots the real Treatment host and applies its EF Core migrations.
        using var startupClient = _treatmentFactory.CreateClient();
        McpFactory = new McpWithRealTreatmentFactory(_treatmentFactory.Server.CreateHandler());
    }

    public async Task DisposeAsync()
    {
        if (McpFactory is not null)
            await McpFactory.DisposeAsync();
        if (_treatmentFactory is not null)
            await _treatmentFactory.DisposeAsync();
        await _database.DisposeAsync();
    }
}

public sealed class RealTreatmentFactory(string connectionString) : WebApplicationFactory<TreatmentProgram>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<TreatmentDbContext>>();
            services.AddDbContext<TreatmentDbContext>(options => options.UseNpgsql(connectionString));
            services.RemoveAll<IHostedService>();
        });
    }
}

public sealed class McpWithRealTreatmentFactory(HttpMessageHandler treatmentHandler)
    : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IServiceAccessTokenProvider>();
            services.AddSingleton<IServiceAccessTokenProvider>(
                new FixedServiceAccessTokenProvider(CreateAdminServiceToken()));
            services
                .AddHttpClient<TreatmentServiceClient>(client =>
                {
                    client.BaseAddress = new Uri("http://real-treatment-service/");
                    client.Timeout = TimeSpan.FromSeconds(15);
                })
                .ConfigurePrimaryHttpMessageHandler(() => treatmentHandler);
        });
    }

    private static string CreateAdminServiceToken()
    {
        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(
                "dev-only-signing-key-change-me-32-chars-minimum!!")),
            SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: "appointment-service",
            audience: "petcare",
            claims:
            [
                new Claim(JwtRegisteredClaimNames.Sub, "mcp-server"),
                new Claim(ClaimTypes.Role, "admin")
            ],
            expires: DateTime.UtcNow.AddMinutes(10),
            signingCredentials: credentials);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}

public sealed class TreatmentMcpEndToEndTests(TreatmentMcpEndToEndFixture fixture)
    : IClassFixture<TreatmentMcpEndToEndFixture>
{
    private static readonly Guid PetId = Guid.Parse("a9999999-9999-9999-9999-999999999999");
    private static readonly Guid OwnerId = Guid.Parse("19999999-9999-9999-9999-999999999999");
    private static readonly Guid VeterinarianId = Guid.Parse("29999999-9999-9999-9999-999999999999");

    [Fact]
    public async Task RecordThenReadMedicalExamination_TraversesMcpTreatmentApiAndPostgreSql()
    {
        using var client = fixture.McpFactory.CreateClient();

        using var recordPayload = await CallToolAsync(client, "record_medical_examination", new
        {
            petId = PetId,
            ownerId = OwnerId,
            veterinarianId = VeterinarianId,
            appointmentId = (Guid?)null,
            examinedAtUtc = DateTimeOffset.Parse("2026-08-14T09:00:00Z"),
            diagnosis = "MCP end-to-end diagnosis",
            treatmentPlan = "Rest and hydration",
            medications = new[] { "Medicine A" },
            nextControlAtUtc = DateTimeOffset.Parse("2026-08-21T09:00:00Z"),
            notes = "Created through MCP"
        });
        var recordedText = GetToolText(recordPayload);

        AssertToolSucceeded(recordPayload);
        Assert.Contains("MCP end-to-end diagnosis", recordedText);
        Assert.Contains(PetId.ToString(), recordedText);

        using var historyPayload = await CallToolAsync(
            client,
            "get_medical_history",
            new { petId = PetId });
        var historyText = GetToolText(historyPayload);

        AssertToolSucceeded(historyPayload);
        Assert.Contains("MCP end-to-end diagnosis", historyText);
        Assert.Contains("Rest and hydration", historyText);
        Assert.Contains("Medicine A", historyText);
    }

    private static async Task<JsonDocument> CallToolAsync(
        HttpClient client,
        string toolName,
        object arguments)
    {
        var json = JsonSerializer.Serialize(new
        {
            jsonrpc = "2.0",
            id = Guid.NewGuid().ToString("N"),
            method = "tools/call",
            @params = new { name = toolName, arguments }
        });
        using var request = new HttpRequestMessage(HttpMethod.Post, "/mcp")
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/event-stream"));
        request.Headers.TryAddWithoutValidation("MCP-Protocol-Version", "2025-11-25");

        using var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        var dataLine = body.Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Single(line => line.StartsWith("data: ", StringComparison.Ordinal));
        return JsonDocument.Parse(dataLine[6..]);
    }

    private static string GetToolText(JsonDocument payload) =>
        payload.RootElement.GetProperty("result").GetProperty("content")[0].GetProperty("text").GetString()
        ?? string.Empty;

    private static void AssertToolSucceeded(JsonDocument payload)
    {
        var result = payload.RootElement.GetProperty("result");
        Assert.False(result.TryGetProperty("isError", out var isError) && isError.GetBoolean());
    }

}
