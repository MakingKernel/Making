using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Making.EntityFrameworkCore.EntityFrameworkCore.Improvements.ConceptProcessors;

/// <summary>
/// Interface for concept processors that handle cross-cutting concerns in EF Core
/// Concept processors are executed during SaveChanges to apply business rules,
/// auditing, multi-tenancy, soft delete, and other cross-cutting concerns
/// </summary>
public interface IConceptProcessor
{
    /// <summary>
    /// Execution order - lower values execute first
    /// Recommended order:
    /// - MultiTenancy: 100
    /// - Auditing: 200  
    /// - SoftDelete: 300
    /// - Custom business rules: 400+
    /// </summary>
    int Order { get; }

    /// <summary>
    /// Process entity entries during SaveChanges
    /// </summary>
    /// <param name="entries">All tracked entity entries</param>
    /// <param name="serviceProvider">Service provider for dependency resolution</param>
    void ProcessEntries(List<EntityEntry> entries, IServiceProvider serviceProvider);
}

/// <summary>
/// Base class for concept processors with common functionality
/// </summary>
public abstract class ConceptProcessorBase : IConceptProcessor
{
    public abstract int Order { get; }

    public virtual void ProcessEntries(List<EntityEntry> entries, IServiceProvider serviceProvider)
    {
        var relevantEntries = GetRelevantEntries(entries);
        if (relevantEntries.Any())
        {
            ProcessRelevantEntries(relevantEntries, serviceProvider);
        }
    }

    /// <summary>
    /// Filter entries that are relevant to this processor
    /// </summary>
    protected abstract IEnumerable<EntityEntry> GetRelevantEntries(List<EntityEntry> entries);

    /// <summary>
    /// Process the filtered relevant entries
    /// </summary>
    protected abstract void ProcessRelevantEntries(IEnumerable<EntityEntry> entries, IServiceProvider serviceProvider);

    /// <summary>
    /// Helper method to check if entry is in specific states
    /// </summary>
    protected static bool IsInStates(EntityEntry entry, params Microsoft.EntityFrameworkCore.EntityState[] states)
    {
        return states.Contains(entry.State);
    }

    /// <summary>
    /// Helper method to check if entity implements an interface
    /// </summary>
    protected static bool ImplementsInterface<TInterface>(EntityEntry entry)
    {
        return typeof(TInterface).IsAssignableFrom(entry.Entity.GetType());
    }
}