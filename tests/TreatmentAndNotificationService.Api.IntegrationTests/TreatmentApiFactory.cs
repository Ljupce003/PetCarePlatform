using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using TreatmentAndNotificationService.Infrastructure.Persistence;

namespace TreatmentAndNotificationService.Api.IntegrationTests;

/// <summary>
/// Boots the production API composition against Testcontainers PostgreSQL. The worker is removed
/// to keep notification state deterministic; all controllers, handlers, repositories, mappings,
/// middleware, migrations, and Npgsql queries are otherwise real production components.
/// </summary>
public sealed class TreatmentApiFactory(string connectionString) : WebApplicationFactory<Program>
{
    private const string JwtIssuer = "appointment-service";
    private const string JwtAudience = "petcare";
    private const string JwtSigningKey = "dev-only-signing-key-change-me-32-chars-minimum!!";

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

    public async Task ResetDatabaseAsync()
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TreatmentDbContext>();
        await db.Database.ExecuteSqlRawAsync("TRUNCATE TABLE notifications, medical_examinations, vaccinations;");
    }

    public async Task<TResult> WithDbContextAsync<TResult>(Func<TreatmentDbContext, Task<TResult>> action)
    {
        using var scope = Services.CreateScope();
        return await action(scope.ServiceProvider.GetRequiredService<TreatmentDbContext>());
    }

    public HttpClient CreateAuthenticatedClient(string role)
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", CreateToken(role));
        return client;
    }

    private static string CreateToken(string role)
    {
        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(JwtSigningKey)),
            SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: JwtIssuer,
            audience: JwtAudience,
            claims:
            [
                new Claim(JwtRegisteredClaimNames.Sub, $"{role}-integration-test"),
                new Claim(ClaimTypes.Role, role)
            ],
            expires: DateTime.UtcNow.AddMinutes(10),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
