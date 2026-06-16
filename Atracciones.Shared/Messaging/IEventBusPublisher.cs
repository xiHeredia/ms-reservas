namespace Atracciones.Shared.Messaging;

public interface IEventBusPublisher
{
    Task PublishAsync(string routingKey, object payload, string? correlationId, CancellationToken cancellationToken = default);
}
