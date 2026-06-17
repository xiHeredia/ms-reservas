namespace Atracciones.Shared.Messaging;

public class RabbitMqOptions
{
    public bool Enabled { get; set; }
    public string? Uri { get; set; }
    public bool UseSsl { get; set; }
    public string HostName { get; set; } = "localhost";
    public int Port { get; set; } = 5672;
    public string UserName { get; set; } = "guest";
    public string Password { get; set; } = "guest";
    public string VirtualHost { get; set; } = "/";
    public string ExchangeName { get; set; } = "atracciones.events";
}
