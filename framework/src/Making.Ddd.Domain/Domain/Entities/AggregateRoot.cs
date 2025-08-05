using Making.Ddd.Domain.Domain.Events;

namespace Making.Ddd.Domain.Domain.Entities;

/// <summary>
/// Base class for aggregate roots.
/// </summary>
[Serializable]
public abstract class AggregateRoot : Entity, IAggregateRoot, IHasDomainEvents
{
    private readonly List<IDomainEvent> _domainEvents = [];

    /// <inheritdoc/>
    public IReadOnlyList<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    /// <inheritdoc/>
    public void AddDomainEvent(IDomainEvent domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }

    /// <inheritdoc/>
    public void RemoveDomainEvent(IDomainEvent domainEvent)
    {
        _domainEvents.Remove(domainEvent);
    }

    /// <inheritdoc/>
    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }
}

/// <summary>
/// Base class for aggregate roots with a typed key.
/// </summary>
/// <typeparam name="TKey">The type of the primary key.</typeparam>
[Serializable]
public abstract class AggregateRoot<TKey> : Entity<TKey>, IAggregateRoot<TKey>, IHasDomainEvents
{
    private readonly List<IDomainEvent> _domainEvents = [];

    /// <summary>
    /// Initializes a new instance of the AggregateRoot class.
    /// </summary>
    protected AggregateRoot()
    {
    }

    /// <summary>
    /// Initializes a new instance of the AggregateRoot class with the specified key.
    /// </summary>
    /// <param name="id">The primary key.</param>
    protected AggregateRoot(TKey id) : base(id)
    {
    }

    /// <inheritdoc/>
    public IReadOnlyList<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    /// <inheritdoc/>
    public void AddDomainEvent(IDomainEvent domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }

    /// <inheritdoc/>
    public void RemoveDomainEvent(IDomainEvent domainEvent)
    {
        _domainEvents.Remove(domainEvent);
    }

    /// <inheritdoc/>
    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }
}