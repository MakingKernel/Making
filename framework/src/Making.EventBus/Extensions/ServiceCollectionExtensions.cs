using Making.EventBus.Abstractions.EventBus;
using Microsoft.Extensions.DependencyInjection;
using Making.Events.Local;

namespace Making.Events.Extensions;

/// <summary>
/// Extension methods for configuring the event bus in the service collection.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds the local event bus implementation to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for method chaining.</returns>
    public static IServiceCollection AddLocalEventBus(this IServiceCollection services)
    {
        services.AddSingleton<IEventBus, LocalEventBus>();
        return services;
    }
}