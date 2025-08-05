namespace Making.Ddd.Domain.Domain.Events;

/// <summary>
/// Base class for domain events.
/// </summary>
[Serializable]
public abstract class DomainEvent : IDomainEvent
{
    /// <summary>
    /// Initializes a new instance of the DomainEvent class.
    /// </summary>
    protected DomainEvent()
    {
        EventId = Guid.NewGuid();
        OccurredAt = DateTime.UtcNow;
    }

    /// <inheritdoc/>
    public DateTime OccurredAt { get; private set; }

    /// <inheritdoc/>
    public Guid EventId { get; private set; }
}