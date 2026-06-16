using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace Atracciones.Shared.Messaging;

public class RabbitMqEventBusPublisher : IEventBusPublisher
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly RabbitMqOptions _options;
    private readonly ILogger<RabbitMqEventBusPublisher> _logger;

    public RabbitMqEventBusPublisher(IOptions<RabbitMqOptions> options, ILogger<RabbitMqEventBusPublisher> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public Task PublishAsync(string routingKey, object payload, string? correlationId, CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled)
            return Task.CompletedTask;

        try
        {
            var factory = new ConnectionFactory
            {
                HostName = _options.HostName,
                Port = _options.Port,
                UserName = _options.UserName,
                Password = _options.Password,
                VirtualHost = _options.VirtualHost,
                DispatchConsumersAsync = true
            };

            using var connection = factory.CreateConnection();
            using var channel = connection.CreateModel();
            channel.ExchangeDeclare(_options.ExchangeName, ExchangeType.Topic, durable: true, autoDelete: false);

            var integrationEvent = new IntegrationEvent
            {
                EventType = routingKey,
                CorrelationId = correlationId,
                Payload = payload
            };

            var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(integrationEvent, JsonOptions));
            var properties = channel.CreateBasicProperties();
            properties.Persistent = true;
            properties.ContentType = "application/json";
            properties.CorrelationId = correlationId;
            properties.MessageId = integrationEvent.EventId.ToString();
            properties.Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds());

            channel.BasicPublish(_options.ExchangeName, routingKey, mandatory: false, basicProperties: properties, body: body);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "No se pudo publicar el evento {RoutingKey} en RabbitMQ.", routingKey);
        }

        return Task.CompletedTask;
    }
}
