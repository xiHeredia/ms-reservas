using System.Text.Json.Serialization;

namespace Atracciones.Shared.Messaging;

public class IntegrationEvent
{
    [JsonPropertyName("event_id")]
    public Guid EventId { get; set; } = Guid.NewGuid();

    [JsonPropertyName("event_type")]
    public string EventType { get; set; } = null!;

    [JsonPropertyName("timestamp")]
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;

    [JsonPropertyName("correlation_id")]
    public string? CorrelationId { get; set; }

    [JsonPropertyName("payload")]
    public object? Payload { get; set; }
}
