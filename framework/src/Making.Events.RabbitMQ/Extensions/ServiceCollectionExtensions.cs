using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Making.RabbitMQ.Extensions;

namespace Making.Events.RabbitMQ.Extensions;

/// <summary>
/// Extension methods for configuring the RabbitMQ event bus.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds the RabbitMQ event bus implementation to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration.</param>
    /// <returns>The service collection for method chaining.</returns>
    public static IServiceCollection AddRabbitMqEventBus(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddRabbitMq(configuration);
        services.AddHostedService<RabbitMqEventBus>();
        services.AddSingleton<IEventBus, RabbitMqEventBus>();
        return services;
    }
}