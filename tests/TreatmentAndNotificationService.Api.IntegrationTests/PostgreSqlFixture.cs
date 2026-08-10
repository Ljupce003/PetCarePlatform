using Testcontainers.PostgreSql;
using Xunit;

namespace TreatmentAndNotificationService.Api.IntegrationTests;

/// <summary>One disposable PostgreSQL instance for the suite; each test truncates its own data.</summary>
public sealed class PostgreSqlFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:17-alpine")
        .WithDatabase("treatment_integration_tests")
        .WithUsername("treatment_tests")
        .WithPassword("treatment_tests")
        .Build();

    public string ConnectionString => _container.GetConnectionString();

    public Task InitializeAsync() => _container.StartAsync();

    public Task DisposeAsync() => _container.DisposeAsync().AsTask();
}

[CollectionDefinition(Name)]
public sealed class PostgreSqlCollection : ICollectionFixture<PostgreSqlFixture>
{
    public const string Name = "treatment-postgresql";
}
