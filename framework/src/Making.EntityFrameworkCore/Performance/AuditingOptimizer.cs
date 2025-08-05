using Making.Security.Users;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;
using Mark.Auditing.Abstractions;

namespace Making.EntityFrameworkCore.Performance;

/// <summary>
/// High-performance auditing optimizer using compiled expressions and caching.
/// </summary>
public class AuditingOptimizer
{
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<AuditingOptimizer> _logger;

    // Cache for compiled property setters
    private static readonly ConcurrentDictionary<string, Delegate> _propertySetters = new();
    private static readonly ConcurrentDictionary<Type, AuditingMetadata> _auditingMetadata = new();

    public AuditingOptimizer(ICurrentUser currentUser, ILogger<AuditingOptimizer> logger)
    {
        _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Applies auditing concepts to entity entries with optimized performance.
    /// </summary>
    /// <param name="entries">The entity entries to process.</param>
    public void ApplyAuditing(IEnumerable<EntityEntry> entries)
    {
        var now = DateTime.UtcNow;
        var currentUserId = _currentUser.Id;

        foreach (var entry in entries)
        {
            var entityType = entry.Entity.GetType();
            var metadata = GetOrCreateAuditingMetadata(entityType);

            if (!metadata.HasAuditingProperties)
                continue;

            try
            {
                switch (entry.State)
                {
                    case Microsoft.EntityFrameworkCore.EntityState.Added:
                        ApplyCreationAuditing(entry.Entity, metadata, now, currentUserId);
                        break;
                    case Microsoft.EntityFrameworkCore.EntityState.Modified:
                        ApplyModificationAuditing(entry.Entity, metadata, now, currentUserId);
                        break;
                    case Microsoft.EntityFrameworkCore.EntityState.Deleted:
                        ApplyDeletionAuditing(entry.Entity, metadata, now, currentUserId);
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error applying auditing to entity of type {EntityType}", entityType.Name);
            }
        }
    }

    /// <summary>
    /// Applies soft delete concepts to entity entries with optimized performance.
    /// </summary>
    /// <param name="entries">The entity entries to process.</param>
    public void ApplySoftDelete(IEnumerable<EntityEntry> entries)
    {
        var now = DateTime.UtcNow;
        var currentUserId = _currentUser.Id;

        foreach (var entry in entries.Where(e => e.State == Microsoft.EntityFrameworkCore.EntityState.Deleted))
        {
            if (entry.Entity is not ISoftDelete)
                continue;

            try
            {
                var entityType = entry.Entity.GetType();
                var metadata = GetOrCreateAuditingMetadata(entityType);

                // Change state to Modified instead of Deleted
                entry.State = Microsoft.EntityFrameworkCore.EntityState.Modified;

                // Set IsDeleted = true
                ((ISoftDelete)entry.Entity).IsDeleted = true;

                // Set deletion audit properties if available
                if (metadata.DeletionTimeProperty != null)
                {
                    SetPropertyValue(entry.Entity, metadata.DeletionTimeProperty, now);
                }

                if (metadata.DeleterIdProperty != null && currentUserId.HasValue)
                {
                    SetPropertyValue(entry.Entity, metadata.DeleterIdProperty, currentUserId.Value);
                }

                _logger.LogDebug("Applied soft delete to entity of type {EntityType}", entityType.Name);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error applying soft delete to entity of type {EntityType}", entry.Entity.GetType().Name);
            }
        }
    }

    /// <summary>
    /// Applies creation auditing properties.
    /// </summary>
    private void ApplyCreationAuditing(object entity, AuditingMetadata metadata, DateTime now, Guid? currentUserId)
    {
        if (metadata.CreationTimeProperty != null)
        {
            SetPropertyValue(entity, metadata.CreationTimeProperty, now);
        }

        if (metadata.CreatorIdProperty != null && currentUserId.HasValue)
        {
            SetPropertyValue(entity, metadata.CreatorIdProperty, currentUserId.Value);
        }
    }

    /// <summary>
    /// Applies modification auditing properties.
    /// </summary>
    private void ApplyModificationAuditing(object entity, AuditingMetadata metadata, DateTime now, Guid? currentUserId)
    {
        if (metadata.ModificationTimeProperty != null)
        {
            SetPropertyValue(entity, metadata.ModificationTimeProperty, now);
        }

        if (metadata.ModifierIdProperty != null && currentUserId.HasValue)
        {
            SetPropertyValue(entity, metadata.ModifierIdProperty, currentUserId.Value);
        }
    }

    /// <summary>
    /// Applies deletion auditing properties.
    /// </summary>
    private void ApplyDeletionAuditing(object entity, AuditingMetadata metadata, DateTime now, Guid? currentUserId)
    {
        if (metadata.DeletionTimeProperty != null)
        {
            SetPropertyValue(entity, metadata.DeletionTimeProperty, now);
        }

        if (metadata.DeleterIdProperty != null && currentUserId.HasValue)
        {
            SetPropertyValue(entity, metadata.DeleterIdProperty, currentUserId.Value);
        }
    }

    /// <summary>
    /// Sets a property value using a compiled expression for optimal performance.
    /// </summary>
    private void SetPropertyValue(object entity, PropertyInfo property, object value)
    {
        var key = $"{entity.GetType().FullName}.{property.Name}";
        
        var setter = _propertySetters.GetOrAdd(key, _ => CreatePropertySetter(entity.GetType(), property));
        
        if (setter is Action<object, object> typedSetter)
        {
            typedSetter(entity, value);
        }
    }

    /// <summary>
    /// Creates a compiled property setter for optimal performance.
    /// </summary>
    private static Delegate CreatePropertySetter(Type entityType, PropertyInfo property)
    {
        // Create parameters
        var entityParam = Expression.Parameter(typeof(object), "entity");
        var valueParam = Expression.Parameter(typeof(object), "value");

        // Cast entity to correct type
        var castEntity = Expression.Convert(entityParam, entityType);

        // Cast value to property type
        var castValue = Expression.Convert(valueParam, property.PropertyType);

        // Create property access
        var propertyAccess = Expression.Property(castEntity, property);

        // Create assignment
        var assignment = Expression.Assign(propertyAccess, castValue);

        // Compile to delegate
        var lambda = Expression.Lambda<Action<object, object>>(assignment, entityParam, valueParam);
        return lambda.Compile();
    }

    /// <summary>
    /// Gets or creates auditing metadata for a type with caching.
    /// </summary>
    private static AuditingMetadata GetOrCreateAuditingMetadata(Type entityType)
    {
        return _auditingMetadata.GetOrAdd(entityType, type =>
        {
            var metadata = new AuditingMetadata();

            // Check for auditing properties
            metadata.CreationTimeProperty = GetProperty(type, nameof(IHasCreationTime.CreationTime));
            metadata.CreatorIdProperty = GetProperty(type, nameof(IMayHaveCreator.CreatorId));
            metadata.ModificationTimeProperty = GetProperty(type, nameof(IHasModificationTime.LastModificationTime));
            metadata.ModifierIdProperty = GetProperty(type, nameof(IModificationAuditedObject.LastModifierId));
            metadata.DeletionTimeProperty = GetProperty(type, nameof(IHasDeletionTime.DeletionTime));
            metadata.DeleterIdProperty = GetProperty(type, nameof(IDeletionAuditedObject.DeleterId));

            metadata.HasAuditingProperties = metadata.CreationTimeProperty != null ||
                                           metadata.CreatorIdProperty != null ||
                                           metadata.ModificationTimeProperty != null ||
                                           metadata.ModifierIdProperty != null ||
                                           metadata.DeletionTimeProperty != null ||
                                           metadata.DeleterIdProperty != null;

            metadata.IsSoftDelete = typeof(ISoftDelete).IsAssignableFrom(type);

            return metadata;
        });
    }

    /// <summary>
    /// Gets a property by name if it exists and has a setter.
    /// </summary>
    private static PropertyInfo? GetProperty(Type type, string propertyName)
    {
        var property = type.GetProperty(propertyName);
        return property?.CanWrite == true ? property : null;
    }

    /// <summary>
    /// Metadata about auditing properties for a type.
    /// </summary>
    private class AuditingMetadata
    {
        public PropertyInfo? CreationTimeProperty { get; set; }
        public PropertyInfo? CreatorIdProperty { get; set; }
        public PropertyInfo? ModificationTimeProperty { get; set; }
        public PropertyInfo? ModifierIdProperty { get; set; }
        public PropertyInfo? DeletionTimeProperty { get; set; }
        public PropertyInfo? DeleterIdProperty { get; set; }
        public bool HasAuditingProperties { get; set; }
        public bool IsSoftDelete { get; set; }
    }
}

/// <summary>
/// Extensions for optimized auditing operations.
/// </summary>
public static class AuditingOptimizerExtensions
{
    /// <summary>
    /// Applies optimized auditing to change tracker entries.
    /// </summary>
    /// <param name="changeTracker">The change tracker.</param>
    /// <param name="auditingOptimizer">The auditing optimizer.</param>
    public static void ApplyOptimizedAuditing(this ChangeTracker changeTracker, AuditingOptimizer auditingOptimizer)
    {
        var entries = changeTracker.Entries()
            .Where(e => e.State == Microsoft.EntityFrameworkCore.EntityState.Added ||
                       e.State == Microsoft.EntityFrameworkCore.EntityState.Modified ||
                       e.State == Microsoft.EntityFrameworkCore.EntityState.Deleted)
            .ToList();

        if (entries.Count == 0)
            return;

        // Apply soft delete first (changes Deleted to Modified)
        auditingOptimizer.ApplySoftDelete(entries);

        // Then apply auditing
        auditingOptimizer.ApplyAuditing(entries);
    }
}