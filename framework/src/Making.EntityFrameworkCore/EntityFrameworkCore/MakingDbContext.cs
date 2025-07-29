using Making.Ddd.Domain.Domain.Entities;
using Making.Ddd.Domain.Domain.Events;
using Making.EventBus.Abstractions.EventBus;
using Making.EntityFrameworkCore.EntityFrameworkCore.Auditing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Making.EntityFrameworkCore.EntityFrameworkCore;

/// <summary>
/// Base DbContext class for Making framework with built-in domain events, multi-tenancy, and auditing support.
/// </summary>
public abstract class MakingDbContext : DbContext
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<MakingDbContext> _logger;

    protected MakingDbContext(DbContextOptions options, IServiceProvider serviceProvider) 
        : base(options)
    {
        _serviceProvider = serviceProvider;
        _logger = serviceProvider.GetRequiredService<ILogger<MakingDbContext>>();
    }

    /// <summary>
    /// Saves changes and publishes domain events.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Number of affected records.</returns>
    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            ApplyMakingConcepts();
            
            var domainEntities = GetDomainEntitiesWithEvents();
            var domainEvents = domainEntities.SelectMany(x => x.DomainEvents).ToList();

            var result = await base.SaveChangesAsync(cancellationToken);

            await PublishDomainEventsAsync(domainEvents, cancellationToken);

            ClearDomainEvents(domainEntities);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while saving changes to database");
            throw;
        }
    }

    /// <summary>
    /// Saves changes synchronously and publishes domain events.
    /// </summary>
    /// <returns>Number of affected records.</returns>
    public override int SaveChanges()
    {
        return SaveChangesAsync().ConfigureAwait(false).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Applies Making framework concepts like soft delete, auditing, etc.
    /// </summary>
    protected virtual void ApplyMakingConcepts()
    {
        foreach (var entry in ChangeTracker.Entries())
        {
            ApplySoftDeleteConcept(entry);
            ApplyAuditingConcept(entry);
            ApplyMultiTenancyConcept(entry);
        }
    }

    /// <summary>
    /// Applies soft delete concept to entities.
    /// </summary>
    /// <param name="entry">Entity entry.</param>
    protected virtual void ApplySoftDeleteConcept(EntityEntry entry)
    {
        if (entry.Entity is not ISoftDelete softDeleteEntity) 
            return;

        switch (entry.State)
        {
            case EntityState.Deleted:
                entry.State = EntityState.Modified;
                softDeleteEntity.IsDeleted = true;
                softDeleteEntity.DeletionTime = DateTime.UtcNow;
                
                if (entry.Entity is ISoftDelete)
                {
                    // Set deletion user if available
                    // This would typically come from current user service
                }
                break;
        }
    }

    /// <summary>
    /// Applies auditing concept to entities.
    /// </summary>
    /// <param name="entry">Entity entry.</param>
    protected virtual void ApplyAuditingConcept(EntityEntry entry)
    {
        var now = DateTime.UtcNow;

        switch (entry.State)
        {
            case EntityState.Added:
                if (entry.Entity is IHasCreationTime creationTimeEntity)
                {
                    creationTimeEntity.CreationTime = now;
                }
                break;

            case EntityState.Modified:
                if (entry.Entity is IHasModificationTime modificationTimeEntity)
                {
                    modificationTimeEntity.LastModificationTime = now;
                }
                break;
        }
    }

    /// <summary>
    /// Applies multi-tenancy concept to entities.
    /// </summary>
    /// <param name="entry">Entity entry.</param>
    protected virtual void ApplyMultiTenancyConcept(EntityEntry entry)
    {
        if (entry.Entity is not Making.MultiTenancy.Abstractions.MultiTenancy.IMultiTenant multiTenantEntity) 
            return;

        if (entry.State == EntityState.Added)
        {
            // Set tenant ID from current tenant context
            // This would typically come from current tenant service
            var currentTenant = _serviceProvider.GetService<Making.MultiTenancy.Abstractions.MultiTenancy.ICurrentTenant>();
            if (currentTenant?.Id != null)
            {
                multiTenantEntity.TenantId = currentTenant.Id;
            }
        }
    }

    /// <summary>
    /// Gets all domain entities that have domain events.
    /// </summary>
    /// <returns>List of entities with domain events.</returns>
    protected virtual List<IHasDomainEvents> GetDomainEntitiesWithEvents()
    {
        return ChangeTracker
            .Entries<IHasDomainEvents>()
            .Where(x => x.Entity.DomainEvents.Any())
            .Select(x => x.Entity)
            .ToList();
    }

    /// <summary>
    /// Publishes domain events.
    /// </summary>
    /// <param name="domainEvents">Domain events to publish.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    protected virtual async Task PublishDomainEventsAsync(IEnumerable<IDomainEvent> domainEvents, CancellationToken cancellationToken)
    {
        var eventPublisher = _serviceProvider.GetService<IEventPublisher>();
        if (eventPublisher == null)
        {
            _logger.LogWarning("No event publisher registered, domain events will not be published");
            return;
        }

        foreach (var domainEvent in domainEvents)
        {
            try
            {
                await eventPublisher.PublishAsync(domainEvent, cancellationToken);
                _logger.LogDebug("Published domain event: {EventType}", domainEvent.GetType().Name);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to publish domain event: {EventType}", domainEvent.GetType().Name);
                // Depending on requirements, you might want to throw or continue
            }
        }
    }

    /// <summary>
    /// Clears domain events from entities.
    /// </summary>
    /// <param name="domainEntities">Entities with domain events.</param>
    protected virtual void ClearDomainEvents(IEnumerable<IHasDomainEvents> domainEntities)
    {
        foreach (var entity in domainEntities)
        {
            entity.ClearDomainEvents();
        }
    }
}

