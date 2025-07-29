namespace Making.EventBus.Abstractions.EventBus;

/// <summary>
/// Base interface for all event data.
/// </summary>
public interface IEventData
{
    /// <summary>
    /// Gets the time when the event occurred.
    /// </summary>
    DateTime EventTime { get; }

    /// <summary>
    /// Gets the unique identifier of the event.
    /// </summary>
    Guid EventId { get; }
}

/// <summary>
/// Generic interface for event data with specific event type.
/// </summary>
/// <typeparam name="TEventSource">Type of the event source</typeparam>
public interface IEventData<out TEventSource> : IEventData
{
    /// <summary>
    /// Gets the object which triggers the event (optional).
    /// </summary>
    TEventSource? EventSource { get; }
}