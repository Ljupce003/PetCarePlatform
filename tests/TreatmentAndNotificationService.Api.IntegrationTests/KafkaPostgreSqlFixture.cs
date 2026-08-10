using Testcontainers.Kafka;
using Testcontainers.PostgreSql;
using Xunit;

namespace TreatmentAndNotificationService.Api.IntegrationTests;

/// <summary>Disposable Kafka and PostgreSQL instances dedicated to the event-consumer suite.</summary>
public sealed class KafkaPostgreSqlFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:17-alpine")
        .WithDatabase("treatment_kafka_integration_tests")
        .WithUsername("treatment_kafka_tests")
        .WithPassword("treatment_kafka_tests")
        .Build();

    private readonly KafkaContainer _kafka = new KafkaBuilder("confluentinc/confluent-local:7.5.0")
        .WithKRaft()
        .Build();

    public string ConnectionString => _postgres.GetConnectionString();
    public string KafkaBootstrapAddress => _kafka.GetBootstrapAddress();

    public Task InitializeAsync() => Task.WhenAll(_postgres.StartAsync(), _kafka.StartAsync());

    public Task DisposeAsync() => Task.WhenAll(
        _postgres.DisposeAsync().AsTask(),
        _kafka.DisposeAsync().AsTask());
}

[CollectionDefinition(Name)]
public sealed class KafkaPostgreSqlCollection : ICollectionFixture<KafkaPostgreSqlFixture>
{
    public const string Name = "treatment-kafka-postgresql";
}
