using Making.EventBus.Abstractions.EventBus;

namespace Making.Events.EventData;

/// <summary>
/// Base implementation of event data.
/// </summary>
[Serializable]
public abstract class EventData : IEventData
{
    /// <summary>
    /// Initializes a new instance of the EventData class.
    /// </summary>
    protected EventData()
    {
        EventTime = DateTime.UtcNow;
        EventId = Guid.NewGuid();
    }

    /// <inheritdoc/>
    public DateTime EventTime { get; private set; }

    /// <inheritdoc/>
    public Guid EventId { get; private set; }
}

/// <summary>
/// Generic event data with event source.
/// </summary>
/// <typeparam name="TEventSource">Type of the event source</typeparam>
[Serializable]
public abstract class EventData<TEventSource> : EventData, IEventData<TEventSource>
{
    /// <summary>
    /// Initializes a new instance of the EventData class.
    /// </summary>
    /// <param name="eventSource">The event source.</param>
    protected EventData(TEventSource? eventSource)
    {
        EventSource = eventSource;
    }

    /// <inheritdoc/>
    public TEventSource? EventSource { get; private set; }
}