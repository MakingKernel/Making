using Making.Ddd.Domain.Domain.Entities;
using Making.Ddd.Domain.Domain.Events;
using Making.EventBus.Abstractions.EventBus;
using Making.MultiTenancy.Abstractions.MultiTenancy;
using Making.Security.Users;
using Making.EntityFrameworkCore.Performance;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Mark.Auditing.Abstractions;

namespace Making.EntityFrameworkCore.EntityFrameworkCore;

/// <summary>
/// Base DbContext class for Making framework with built-in domain events, multi-tenancy, and auditing support.
/// </summary>
public abstract class MakingDbContext : DbContext
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<MakingDbContext> _logger;
    private readonly ICurrentUser _currentUser;
    private readonly Lazy<AuditingOptimizer> _auditingOptimizer;

    protected MakingDbContext(DbContextOptions options, IServiceProvider serviceProvider) 
        : base(options)
    {
        _serviceProvider = serviceProvider;
        _logger = serviceProvider.GetRequiredService<ILogger<MakingDbContext>>();
        _currentUser = serviceProvider.GetRequiredService<ICurrentUser>();
        _auditingOptimizer = new Lazy<AuditingOptimizer>(() => 
            new AuditingOptimizer(_currentUser, serviceProvider.GetRequiredService<ILogger<AuditingOptimizer>>()));
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
        // Use optimized auditing and soft delete processing
        ChangeTracker.ApplyOptimizedAuditing(_auditingOptimizer.Value);

        // Apply multi-tenancy concept (this still uses the old approach as it's simpler)
        foreach (var entry in ChangeTracker.Entries().Where(e => e.State == EntityState.Added))
        {
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
                
                var now = DateTime.UtcNow;
                var entityType = entry.Entity.GetType();
                
                // Set deletion time using reflection
                SetPropertyValue(entry.Entity, entityType, nameof(IHasDeletionTime.DeletionTime), now);
                
                // Set deleter ID if available
                var currentUserId = GetCurrentUserId();
                if (currentUserId.HasValue)
                {
                    SetPropertyValue(entry.Entity, entityType, nameof(IDeletionAuditedObject.DeleterId), currentUserId.Value);
                }
                break;
        }
    }

    /// <summary>
    /// Applies auditing concept to entities using reflection.
    /// </summary>
    /// <param name="entry">Entity entry.</param>
    protected virtual void ApplyAuditingConcept(EntityEntry entry)
    {
        var now = DateTime.UtcNow;
        var entity = entry.Entity;
        var entityType = entity.GetType();

        switch (entry.State)
        {
            case EntityState.Added:
                // Set creation time
                SetPropertyValue(entity, entityType, nameof(IHasCreationTime.CreationTime), now);
                
                // Set creator ID if available
                var currentUserId = GetCurrentUserId();
                if (currentUserId.HasValue)
                {
                    SetPropertyValue(entity, entityType, nameof(IMayHaveCreator.CreatorId), currentUserId.Value);
                }
                break;

            case EntityState.Modified:
                // Set modification time
                SetPropertyValue(entity, entityType, nameof(IHasModificationTime.LastModificationTime), now);
                
                // Set modifier ID if available
                var currentModifierId = GetCurrentUserId();
                if (currentModifierId.HasValue)
                {
                    SetPropertyValue(entity, entityType, nameof(IModificationAuditedObject.LastModifierId), currentModifierId.Value);
                }
                break;

            case EntityState.Deleted:
                // This is handled in ApplySoftDeleteConcept, but we can also set deleter info here
                var currentDeleterId = GetCurrentUserId();
                if (currentDeleterId.HasValue)
                {
                    SetPropertyValue(entity, entityType, nameof(IDeletionAuditedObject.DeleterId), currentDeleterId.Value);
                }
                break;
        }
    }

    /// <summary>
    /// Sets a property value using reflection if the property exists and has a setter.
    /// </summary>
    /// <param name="entity">The entity instance.</param>
    /// <param name="entityType">The entity type.</param>
    /// <param name="propertyName">The property name.</param>
    /// <param name="value">The value to set.</param>
    protected virtual void SetPropertyValue(object entity, Type entityType, string propertyName, object value)
    {
        var property = entityType.GetProperty(propertyName);
        if (property != null && property.CanWrite)
        {
            try
            {
                property.SetValue(entity, value);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to set auditing property {PropertyName} on entity {EntityType}", 
                    propertyName, entityType.Name);
            }
        }
    }

    /// <summary>
    /// Gets the current user ID. This should be overridden to provide actual user context.
    /// </summary>
    /// <returns>Current user ID or null if not available.</returns>
    protected virtual Guid? GetCurrentUserId()
    {
        return _currentUser.Id;
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

