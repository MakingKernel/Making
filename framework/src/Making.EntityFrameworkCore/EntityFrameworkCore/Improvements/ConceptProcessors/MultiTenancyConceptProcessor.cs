using Making.MultiTenancy.Abstractions.MultiTenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.Extensions.DependencyInjection;

namespace Making.EntityFrameworkCore.EntityFrameworkCore.Improvements.ConceptProcessors;

/// <summary>
/// Concept processor for handling multi-tenancy concerns
/// Automatically sets tenant ID for new entities and validates tenant isolation
/// </summary>
public class MultiTenancyConceptProcessor : ConceptProcessorBase
{
    public override int Order => 100; // Execute first to ensure tenant isolation

    protected override IEnumerable<EntityEntry> GetRelevantEntries(List<EntityEntry> entries)
    {
        return entries.Where(entry =>
            IsInStates(entry, EntityState.Added, EntityState.Modified, EntityState.Deleted) &&
            ImplementsInterface<IMultiTenant>(entry));
    }

    protected override void ProcessRelevantEntries(IEnumerable<EntityEntry> entries, IServiceProvider serviceProvider)
    {
        var currentTenantAccessor = serviceProvider.GetService<ICurrentTenantAccessor>();
        var currentTenantId = currentTenantAccessor?.TenantId;

        foreach (var entry in entries)
        {
            ProcessMultiTenancyForEntry(entry, currentTenantId);
        }
    }

    private static void ProcessMultiTenancyForEntry(EntityEntry entry, Guid? currentTenantId)
    {
        if (entry.Entity is not IMultiTenant multiTenantEntity)
        {
            return;
        }

        switch (entry.State)
        {
            case EntityState.Added:
                ProcessNewEntity(multiTenantEntity, currentTenantId);
                break;

            case EntityState.Modified:
            case EntityState.Deleted:
                ValidateTenantAccess(multiTenantEntity, currentTenantId, entry);
                break;
        }
    }

    private static void ProcessNewEntity(IMultiTenant entity, Guid? currentTenantId)
    {
        // Auto-assign tenant ID for new entities if not already set
        if (entity.TenantId == null)
        {
            entity.TenantId = currentTenantId;
        }
        else if (currentTenantId.HasValue && entity.TenantId != currentTenantId)
        {
            // Validate that user is not trying to create entity for different tenant
            throw new UnauthorizedAccessException(
                $"Cannot create entity for tenant {entity.TenantId} when current tenant is {currentTenantId}");
        }
    }

    private static void ValidateTenantAccess(IMultiTenant entity, Guid? currentTenantId, EntityEntry entry)
    {
        // Prevent modification of entities from different tenants
        if (currentTenantId.HasValue && entity.TenantId != currentTenantId)
        {
            var operation = entry.State == EntityState.Modified ? "modify" : "delete";
            throw new UnauthorizedAccessException(
                $"Cannot {operation} entity from tenant {entity.TenantId} when current tenant is {currentTenantId}");
        }

        // Prevent changing tenant ID on existing entities
        if (entry.State == EntityState.Modified)
        {
            var tenantIdProperty = entry.Property(nameof(IMultiTenant.TenantId));
            if (tenantIdProperty.IsModified)
            {
                throw new InvalidOperationException("Cannot change TenantId of existing entity");
            }
        }
    }
}

/// <summary>
/// Interface for accessing current tenant context
/// This should be implemented by the consuming application
/// </summary>
public interface ICurrentTenantAccessor
{
    /// <summary>
    /// Gets the current tenant ID
    /// </summary>
    Guid? TenantId { get; }

    /// <summary>
    /// Gets the current tenant name
    /// </summary>
    string? TenantName { get; }
}

/// <summary>
/// Default implementation that can be used when multi-tenancy is not active
/// </summary>
public class NullCurrentTenantAccessor : ICurrentTenantAccessor
{
    public Guid? TenantId => null;
    public string? TenantName => null;
}