using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Atracciones.Shared.Messaging;

public static class RabbitMqServiceCollectionExtensions
{
    public static IServiceCollection AddRabbitMqEventBus(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<RabbitMqOptions>(configuration.GetSection("RabbitMq"));

        var enabled = configuration.GetValue("RabbitMq:Enabled", false);
        if (enabled)
            services.AddSingleton<IEventBusPublisher, RabbitMqEventBusPublisher>();
        else
            services.AddSingleton<IEventBusPublisher, NullEventBusPublisher>();

        return services;
    }
}
