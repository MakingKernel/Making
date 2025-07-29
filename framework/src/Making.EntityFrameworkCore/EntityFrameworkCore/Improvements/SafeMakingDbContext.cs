using Making.Ddd.Domain.Domain.Entities;
using Making.Ddd.Domain.Domain.Events;
using Making.EntityFrameworkCore.EntityFrameworkCore.Improvements.ConceptProcessors;
using Making.EventBus.Abstractions.EventBus;
using Making.MultiTenancy.Abstractions.MultiTenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Making.EntityFrameworkCore.EntityFrameworkCore.Improvements;

/// <summary>
/// Enhanced Making DbContext with pluggable concept processors and improved safety
/// This addresses the deadlock risks and performance issues in the base MakingDbContext
/// </summary>
public abstract class SafeMakingDbContext : DbContext
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<SafeMakingDbContext> _logger;
    private readonly List<IConceptProcessor> _conceptProcessors;
    private readonly SafeMakingDbContextOptions _options;

    protected SafeMakingDbContext(DbContextOptions options, IServiceProvider serviceProvider)
        : base(options)
    {
        _serviceProvider = serviceProvider;
        _logger = serviceProvider.GetRequiredService<ILogger<SafeMakingDbContext>>();
        _conceptProcessors = serviceProvider.GetServices<IConceptProcessor>().ToList();
        _options = serviceProvider.GetService<SafeMakingDbContextOptions>() ?? new SafeMakingDbContextOptions();
    }

    /// <summary>
    /// Synchronous SaveChanges - uses sync-only event publishing to avoid deadlocks
    /// </summary>
    public override int SaveChanges()
    {
        try
        {
            ApplyMakingConcepts();
            var domainEvents = GetDomainEntitiesWithEvents();
            var result = base.SaveChanges();

            // Publish events synchronously to avoid deadlock issues
            if (_options.PublishEventsAfterSave)
            {
                PublishDomainEventsSync(domainEvents);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred during SaveChanges");
            throw;
        }
    }

    /// <summary>
    /// Asynchronous SaveChanges - uses async event publishing
    /// </summary>
    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            ApplyMakingConcepts();
            var domainEvents = GetDomainEntitiesWithEvents();
            var result = await base.SaveChangesAsync(cancellationToken);

            // Publish events asynchronously
            if (_options.PublishEventsAfterSave)
            {
                await PublishDomainEventsAsync(domainEvents, cancellationToken);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred during SaveChangesAsync");
            throw;
        }
    }

    /// <summary>
    /// Apply Making concepts using pluggable processors
    /// </summary>
    private void ApplyMakingConcepts()
    {
        var entries = ChangeTracker.Entries().ToList();

        foreach (var processor in _conceptProcessors.OrderBy(p => p.Order))
        {
            processor.ProcessEntries(entries, _serviceProvider);
        }
    }

    /// <summary>
    /// Get entities with domain events before clearing them
    /// </summary>
    private List<(IHasDomainEvents Entity, List<IDomainEvent> Events)> GetDomainEntitiesWithEvents()
    {
        var entitiesWithEvents = ChangeTracker.Entries<IHasDomainEvents>()
            .Where(e => e.Entity.DomainEvents.Any())
            .Select(e => (e.Entity, e.Entity.DomainEvents.ToList()))
            .ToList();

        // Clear events from entities to prevent re-publishing
        foreach (var (entity, _) in entitiesWithEvents)
        {
            entity.ClearDomainEvents();
        }

        return entitiesWithEvents;
    }

    /// <summary>
    /// Publish domain events synchronously (for sync SaveChanges)
    /// </summary>
    private void PublishDomainEventsSync(List<(IHasDomainEvents Entity, List<IDomainEvent> Events)> entitiesWithEvents)
    {
        var eventPublisher = _serviceProvider.GetService<IEventPublisher>();
        if (eventPublisher == null) return;

        foreach (var (_, events) in entitiesWithEvents)
        {
            foreach (var domainEvent in events)
            {
                try
                {
                    // Use synchronous publishing method if available
                    if (eventPublisher is IEventPublisherSync syncPublisher)
                    {
                        syncPublisher.PublishSync(domainEvent);
                    }
                    else
                    {
                        // Fallback: block on async method (not ideal but necessary for sync context)
                        eventPublisher.PublishAsync(domainEvent).GetAwaiter().GetResult();
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error publishing domain event {EventType}", domainEvent.GetType().Name);
                    if (_options.ThrowOnEventPublishFailure)
                    {
                        throw;
                    }
                }
            }
        }
    }

    /// <summary>
    /// Publish domain events asynchronously (for async SaveChangesAsync)
    /// </summary>
    private async Task PublishDomainEventsAsync(
        List<(IHasDomainEvents Entity, List<IDomainEvent> Events)> entitiesWithEvents,
        CancellationToken cancellationToken)
    {
        var eventPublisher = _serviceProvider.GetService<IEventPublisher>();
        if (eventPublisher == null) return;

        foreach (var (_, events) in entitiesWithEvents)
        {
            foreach (var domainEvent in events)
            {
                try
                {
                    await eventPublisher.PublishAsync(domainEvent, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error publishing domain event {EventType}", domainEvent.GetType().Name);
                    if (_options.ThrowOnEventPublishFailure)
                    {
                        throw;
                    }
                }
            }
        }
    }

    /// <summary>
    /// Configure entity framework core with Making conventions
    /// </summary>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply global query filters for soft delete
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(ISoftDelete).IsAssignableFrom(entityType.ClrType))
            {
                var method = typeof(SafeMakingDbContext)
                    .GetMethod(nameof(SetSoftDeleteFilter), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)?
                    .MakeGenericMethod(entityType.ClrType);
                method?.Invoke(null, new object[] { modelBuilder });
            }

            // Apply multi-tenancy filters
            if (typeof(IMultiTenant).IsAssignableFrom(entityType.ClrType))
            {
                var method = typeof(SafeMakingDbContext)
                    .GetMethod(nameof(SetMultiTenancyFilter), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)?
                    .MakeGenericMethod(entityType.ClrType);
                method?.Invoke(null, new object[] { modelBuilder });
            }
        }
    }

    private static void SetSoftDeleteFilter<T>(ModelBuilder modelBuilder) where T : class, ISoftDelete
    {
        modelBuilder.Entity<T>().HasQueryFilter(e => !e.IsDeleted);
    }

    private static void SetMultiTenancyFilter<T>(ModelBuilder modelBuilder) where T : class, IMultiTenant
    {
        // Note: Multi-tenancy filter would need tenant context to be properly implemented
        // This is a placeholder for the filter structure
        // modelBuilder.Entity<T>().HasQueryFilter(e => e.TenantId == currentTenantId);
    }
}

/// <summary>
/// Configuration options for SafeMakingDbContext
/// </summary>
public class SafeMakingDbContextOptions
{
    /// <summary>
    /// Whether to publish domain events after successful save
    /// Default: true
    /// </summary>
    public bool PublishEventsAfterSave { get; set; } = true;

    /// <summary>
    /// Whether to throw exception when event publishing fails
    /// Default: false (logs error but continues)
    /// </summary>
    public bool ThrowOnEventPublishFailure { get; set; } = false;
}

/// <summary>
/// Extension interface for synchronous event publishing
/// </summary>
public interface IEventPublisherSync
{
    void PublishSync<T>(T eventData) where T : class;
}