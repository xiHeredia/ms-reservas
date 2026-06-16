namespace Atracciones.Shared.Messaging;

public class NullEventBusPublisher : IEventBusPublisher
{
    public Task PublishAsync(string routingKey, object payload, string? correlationId, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}
