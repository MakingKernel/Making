using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Mark.RabbitMQ.Connections;
using Mark.RabbitMQ.Options;

namespace Mark.RabbitMQ.Extensions;

/// <summary>
/// Extension methods for configuring RabbitMQ services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds RabbitMQ services to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration.</param>
    /// <returns>The service collection for method chaining.</returns>
    public static IServiceCollection AddRabbitMq(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<RabbitMqOptions>(configuration.GetSection("RabbitMq"));
        services.TryAddSingleton<IRabbitMqConnection, RabbitMqConnection>();
        return services;
    }

    /// <summary>
    /// Adds RabbitMQ services to the service collection with custom configuration.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configureOptions">The configuration action.</param>
    /// <returns>The service collection for method chaining.</returns>
    public static IServiceCollection AddRabbitMq(this IServiceCollection services, Action<RabbitMqOptions> configureOptions)
    {
        services.Configure(configureOptions);
        services.TryAddSingleton<IRabbitMqConnection, RabbitMqConnection>();
        return services;
    }
}