
namespace Making.EventBus.Abstractions.EventBus;

/// <summary>
/// Defines the interface for an event bus that handles publishing and subscribing to events.
/// </summary>
public interface IEventBus
{
    /// <summary>
    /// Publishes an event asynchronously.
    /// </summary>
    /// <typeparam name="TEvent">The type of the event.</typeparam>
    /// <param name="event">The event to publish.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task PublishAsync<TEvent>(TEvent @event) where TEvent : class;

    /// <summary>
    /// Subscribes to an event type with a handler.
    /// </summary>
    /// <typeparam name="TEvent">The type of the event to subscribe to.</typeparam>
    /// <typeparam name="THandler">The type of the event handler.</typeparam>
    Task Subscribe<TEvent, THandler>()
        where TEvent : class
        where THandler : IEventHandler<TEvent>;

    /// <summary>
    /// Unsubscribes from an event type.
    /// </summary>
    /// <typeparam name="TEvent">The type of the event to unsubscribe from.</typeparam>
    /// <typeparam name="THandler">The type of the event handler.</typeparam>
    Task Unsubscribe<TEvent, THandler>()
        where TEvent : class
        where THandler : IEventHandler<TEvent>;
}