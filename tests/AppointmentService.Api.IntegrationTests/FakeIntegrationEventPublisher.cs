using System.Collections.Concurrent;
using Shared.Messaging;

namespace AppointmentService.Api.IntegrationTests;

public sealed record PublishedEvent(string Topic, object Message);

/// <summary>
/// Records every event that would have gone to Kafka, in memory, instead of actually publishing
/// it -- lets integration tests assert "message production behavior" (topic + event shape) for
/// booking/cancel/reschedule without needing a real Kafka broker running.
/// </summary>
public sealed class FakeIntegrationEventPublisher : IIntegrationEventPublisher
{
    private readonly ConcurrentQueue<PublishedEvent> _published = new();

    public IReadOnlyCollection<PublishedEvent> Published => _published.ToArray();

    public Task PublishAsync<T>(string topic, T message, CancellationToken cancellationToken = default)
    {
        _published.Enqueue(new PublishedEvent(topic, message!));
        return Task.CompletedTask;
    }

    public void Clear() => _published.Clear();
}
