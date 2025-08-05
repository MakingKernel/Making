namespace Making.EventBus.Abstractions.EventBus;

/// <summary>
/// Defines the interface for subscribing to events.
/// </summary>
public interface IEventSubscriber
{
    /// <summary>
    /// Subscribes to an event with a handler.
    /// </summary>
    /// <typeparam name="TEvent">The type of the event.</typeparam>
    /// <typeparam name="THandler">The type of the handler.</typeparam>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task SubscribeAsync<TEvent, THandler>(CancellationToken cancellationToken = default)
        where TEvent : class
        where THandler : IEventHandler<TEvent>;

    /// <summary>
    /// Unsubscribes from an event.
    /// </summary>
    /// <typeparam name="TEvent">The type of the event.</typeparam>
    /// <typeparam name="THandler">The type of the handler.</typeparam>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task UnsubscribeAsync<TEvent, THandler>(CancellationToken cancellationToken = default)
        where TEvent : class
        where THandler : IEventHandler<TEvent>;
}