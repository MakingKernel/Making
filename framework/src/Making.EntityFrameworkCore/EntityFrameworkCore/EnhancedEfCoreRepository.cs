using Making.Ddd.Domain.Domain.Entities;
using Making.Ddd.Domain.Domain.Repositories;
using Making.EntityFrameworkCore.Query;
using Making.EntityFrameworkCore.Performance;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System.Linq.Expressions;
using Mark.Auditing.Abstractions;

namespace Making.EntityFrameworkCore.EntityFrameworkCore;

/// <summary>
/// Enhanced Entity Framework Core repository with bulk operations, specifications, caching, and batch processing.
/// </summary>
/// <typeparam name="TEntity">The entity type.</typeparam>
public class EnhancedEfCoreRepository<TEntity> : EfCoreRepository<TEntity>, IBatchProcessorFactory<TEntity> 
    where TEntity : class, IEntity
{
    protected readonly IMemoryCache? MemoryCache;

    public EnhancedEfCoreRepository(
        MakingDbContext dbContext, 
        ILogger<EnhancedEfCoreRepository<TEntity>> logger,
        IMemoryCache? memoryCache = null) 
        : base(dbContext, logger)
    {
        MemoryCache = memoryCache;
    }

    /// <summary>
    /// Gets a query builder for building complex queries.
    /// </summary>
    /// <returns>A query builder instance.</returns>
    public virtual IQueryBuilder<TEntity> Query()
    {
        var query = GetQueryableWithSoftDeleteFilter();
        return new EfCoreQueryBuilder<TEntity>(query, Logger as ILogger<EfCoreQueryBuilder<TEntity>> ?? 
            throw new InvalidOperationException("Logger type mismatch"));
    }

    /// <summary>
    /// Gets entities using a specification.
    /// </summary>
    /// <param name="specification">The specification to apply.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A list of entities matching the specification.</returns>
    public virtual async Task<List<TEntity>> GetBySpecificationAsync(ISpecification<TEntity> specification, CancellationToken cancellationToken = default)
    {
        try
        {
            // Check cache first if caching is enabled
            if (MemoryCache != null && !string.IsNullOrEmpty(specification.CacheKey))
            {
                if (MemoryCache.TryGetValue(specification.CacheKey, out List<TEntity>? cachedResult))
                {
                    Logger.LogDebug("Retrieved {Count} entities from cache for {EntityType}", cachedResult?.Count ?? 0, typeof(TEntity).Name);
                    return cachedResult ?? new List<TEntity>();
                }
            }

            var result = await Query()
                .ApplySpecification(specification)
                .ToListAsync(cancellationToken);

            // Cache the result if caching is enabled
            if (MemoryCache != null && !string.IsNullOrEmpty(specification.CacheKey))
            {
                var cacheOptions = new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = specification.CacheExpiration ?? TimeSpan.FromMinutes(30)
                };
                MemoryCache.Set(specification.CacheKey, result, cacheOptions);
                Logger.LogDebug("Cached {Count} entities for {EntityType} with key {CacheKey}", result.Count, typeof(TEntity).Name, specification.CacheKey);
            }

            Logger.LogDebug("Retrieved {Count} entities by specification for {EntityType}", result.Count, typeof(TEntity).Name);
            return result;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error retrieving entities by specification for {EntityType}", typeof(TEntity).Name);
            throw;
        }
    }

    /// <summary>
    /// Gets a single entity using a specification.
    /// </summary>
    /// <param name="specification">The specification to apply.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The entity matching the specification or null.</returns>
    public virtual async Task<TEntity?> GetSingleBySpecificationAsync(ISpecification<TEntity> specification, CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await Query()
                .ApplySpecification(specification)
                .FirstOrDefaultAsync(cancellationToken);

            Logger.LogDebug("Retrieved single entity by specification for {EntityType}, found: {Found}", typeof(TEntity).Name, result != null);
            return result;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error retrieving single entity by specification for {EntityType}", typeof(TEntity).Name);
            throw;
        }
    }

    /// <summary>
    /// Gets the count of entities matching a specification.
    /// </summary>
    /// <param name="specification">The specification to apply.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The count of matching entities.</returns>
    public virtual async Task<long> GetCountBySpecificationAsync(ISpecification<TEntity> specification, CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await Query()
                .ApplySpecification(specification)
                .LongCountAsync(cancellationToken);

            Logger.LogDebug("Counted {Count} entities by specification for {EntityType}", result, typeof(TEntity).Name);
            return result;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error counting entities by specification for {EntityType}", typeof(TEntity).Name);
            throw;
        }
    }

    /// <summary>
    /// Gets a paged result using a specification.
    /// </summary>
    /// <param name="specification">The specification to apply.</param>
    /// <param name="pageNumber">The page number (1-based).</param>
    /// <param name="pageSize">The page size.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A paged result containing entities and pagination metadata.</returns>
    public virtual async Task<PagedResult<TEntity>> GetPagedBySpecificationAsync(ISpecification<TEntity> specification, int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        try
        {
            var queryBuilder = Query().ApplySpecification(specification);
            var result = await queryBuilder.ToPagedListAsync(pageNumber, pageSize, cancellationToken);

            Logger.LogDebug("Retrieved paged result for {EntityType}: Page {PageNumber}, Size {PageSize}, Total {TotalCount}", 
                typeof(TEntity).Name, pageNumber, pageSize, result.TotalCount);
            return result;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error retrieving paged entities by specification for {EntityType}", typeof(TEntity).Name);
            throw;
        }
    }

    /// <summary>
    /// Inserts multiple entities in a single batch operation.
    /// </summary>
    /// <param name="entities">The entities to insert.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The inserted entities.</returns>
    public virtual async Task<List<TEntity>> BulkInsertAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default)
    {
        try
        {
            var entityList = entities.ToList();
            await DbSet.AddRangeAsync(entityList, cancellationToken);
            Logger.LogDebug("Bulk inserted {Count} entities for {EntityType}", entityList.Count, typeof(TEntity).Name);
            
            // Invalidate cache if necessary
            InvalidateEntityCache();
            
            return entityList;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error bulk inserting entities for {EntityType}", typeof(TEntity).Name);
            throw;
        }
    }

    /// <summary>
    /// Updates multiple entities in a single batch operation.
    /// </summary>
    /// <param name="entities">The entities to update.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The updated entities.</returns>
    public virtual async Task<List<TEntity>> BulkUpdateAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default)
    {
        try
        {
            var entityList = entities.ToList();
            DbSet.UpdateRange(entityList);
            Logger.LogDebug("Bulk updated {Count} entities for {EntityType}", entityList.Count, typeof(TEntity).Name);
            
            // Invalidate cache if necessary
            InvalidateEntityCache();
            
            return entityList;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error bulk updating entities for {EntityType}", typeof(TEntity).Name);
            throw;
        }
    }

    /// <summary>
    /// Deletes multiple entities in a single batch operation.
    /// </summary>
    /// <param name="entities">The entities to delete.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public virtual async Task BulkDeleteAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default)
    {
        try
        {
            var entityList = entities.ToList();
            DbSet.RemoveRange(entityList);
            Logger.LogDebug("Bulk deleted {Count} entities for {EntityType}", entityList.Count, typeof(TEntity).Name);
            
            // Invalidate cache if necessary
            InvalidateEntityCache();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error bulk deleting entities for {EntityType}", typeof(TEntity).Name);
            throw;
        }
    }

    /// <summary>
    /// Gets entities with automatic soft delete filtering applied.
    /// </summary>
    /// <param name="predicate">The filter predicate (optional).</param>
    /// <param name="includeDeleted">Whether to include soft-deleted entities.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A list of entities.</returns>
    public virtual async Task<List<TEntity>> GetWithSoftDeleteFilterAsync(Expression<Func<TEntity, bool>>? predicate = null, bool includeDeleted = false, CancellationToken cancellationToken = default)
    {
        try
        {
            var queryBuilder = includeDeleted ? 
                new EfCoreQueryBuilder<TEntity>(DbSet.AsQueryable(), Logger as ILogger<EfCoreQueryBuilder<TEntity>> ?? throw new InvalidOperationException("Logger type mismatch")) :
                Query();

            if (predicate != null)
            {
                queryBuilder = queryBuilder.Where(predicate);
            }

            var result = await queryBuilder.ToListAsync(cancellationToken);
            Logger.LogDebug("Retrieved {Count} entities with soft delete filter for {EntityType}, includeDeleted: {IncludeDeleted}", 
                result.Count, typeof(TEntity).Name, includeDeleted);
            return result;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error retrieving entities with soft delete filter for {EntityType}", typeof(TEntity).Name);
            throw;
        }
    }

    /// <summary>
    /// Gets entities with optional caching.
    /// </summary>
    /// <param name="predicate">The filter predicate (optional).</param>
    /// <param name="cacheKey">The cache key (optional).</param>
    /// <param name="cacheExpiration">The cache expiration time (optional).</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A list of entities.</returns>
    public virtual async Task<List<TEntity>> GetWithCacheAsync(Expression<Func<TEntity, bool>>? predicate = null, string? cacheKey = null, TimeSpan? cacheExpiration = null, CancellationToken cancellationToken = default)
    {
        try
        {
            // Check cache first if caching is enabled
            if (MemoryCache != null && !string.IsNullOrEmpty(cacheKey))
            {
                if (MemoryCache.TryGetValue(cacheKey, out List<TEntity>? cachedResult))
                {
                    Logger.LogDebug("Retrieved {Count} entities from cache for {EntityType}", cachedResult?.Count ?? 0, typeof(TEntity).Name);
                    return cachedResult ?? new List<TEntity>();
                }
            }

            var queryBuilder = Query();
            if (predicate != null)
            {
                queryBuilder = queryBuilder.Where(predicate);
            }

            var result = await queryBuilder.ToListAsync(cancellationToken);

            // Cache the result if caching is enabled
            if (MemoryCache != null && !string.IsNullOrEmpty(cacheKey))
            {
                var cacheOptions = new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = cacheExpiration ?? TimeSpan.FromMinutes(30)
                };
                MemoryCache.Set(cacheKey, result, cacheOptions);
                Logger.LogDebug("Cached {Count} entities for {EntityType} with key {CacheKey}", result.Count, typeof(TEntity).Name, cacheKey);
            }

            Logger.LogDebug("Retrieved {Count} entities with cache for {EntityType}", result.Count, typeof(TEntity).Name);
            return result;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error retrieving entities with cache for {EntityType}", typeof(TEntity).Name);
            throw;
        }
    }

    /// <summary>
    /// Projects entities to a different type with specification support.
    /// </summary>
    /// <typeparam name="TResult">The result type.</typeparam>
    /// <param name="specification">The specification to apply.</param>
    /// <param name="projection">The projection expression.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A list of projected results.</returns>
    public virtual async Task<List<TResult>> ProjectToAsync<TResult>(ISpecification<TEntity> specification, Expression<Func<TEntity, TResult>> projection, CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await Query()
                .ApplySpecification(specification)
                .Select(projection)
                .ToListAsync(cancellationToken);

            Logger.LogDebug("Projected {Count} entities from {SourceType} to {ResultType}", 
                result.Count, typeof(TEntity).Name, typeof(TResult).Name);
            return result;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error projecting entities from {SourceType} to {ResultType}", 
                typeof(TEntity).Name, typeof(TResult).Name);
            throw;
        }
    }

    /// <summary>
    /// Checks if any entities exist matching the specification.
    /// </summary>
    /// <param name="specification">The specification to apply.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>True if any entities match; otherwise, false.</returns>
    public virtual async Task<bool> AnyBySpecificationAsync(ISpecification<TEntity> specification, CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await Query()
                .ApplySpecification(specification)
                .AnyAsync(cancellationToken);

            Logger.LogDebug("Checked existence by specification for {EntityType}, result: {Result}", typeof(TEntity).Name, result);
            return result;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error checking existence by specification for {EntityType}", typeof(TEntity).Name);
            throw;
        }
    }

    /// <summary>
    /// Creates a batch processor for high-performance operations.
    /// </summary>
    /// <returns>A new batch processor instance.</returns>
    public virtual IBatchProcessor<TEntity> CreateBatchProcessor()
    {
        return new EfCoreBatchProcessor<TEntity>(DbContext, 
            Logger as ILogger<EfCoreBatchProcessor<TEntity>> ?? 
            throw new InvalidOperationException("Logger type mismatch"));
    }

    /// <summary>
    /// Override the base insert method to invalidate cache.
    /// </summary>
    public override async Task<TEntity> InsertAsync(TEntity entity)
    {
        var result = await base.InsertAsync(entity);
        InvalidateEntityCache();
        return result;
    }

    /// <summary>
    /// Override the base update method to invalidate cache.
    /// </summary>
    public override Task<TEntity> UpdateAsync(TEntity entity)
    {
        var result = base.UpdateAsync(entity);
        InvalidateEntityCache();
        return result;
    }

    /// <summary>
    /// Override the base delete method to invalidate cache.
    /// </summary>
    public override Task DeleteAsync(TEntity entity)
    {
        var result = base.DeleteAsync(entity);
        InvalidateEntityCache();
        return result;
    }

    /// <summary>
    /// Override the base delete method to invalidate cache.
    /// </summary>
    public override async Task DeleteAsync(Expression<Func<TEntity, bool>> predicate)
    {
        await base.DeleteAsync(predicate);
        InvalidateEntityCache();
    }

    /// <summary>
    /// Gets the base queryable with soft delete filtering applied.
    /// </summary>
    /// <returns>The filtered queryable.</returns>
    protected virtual IQueryable<TEntity> GetQueryableWithSoftDeleteFilter()
    {
        var query = DbSet.AsQueryable();
        
        // Apply soft delete filter if the entity implements ISoftDelete
        if (typeof(ISoftDelete).IsAssignableFrom(typeof(TEntity)))
        {
            query = query.Where(e => !((ISoftDelete)e).IsDeleted);
        }
        
        return query;
    }

    /// <summary>
    /// Invalidates cached entries for this entity type.
    /// </summary>
    protected virtual void InvalidateEntityCache()
    {
        // This is a simplified cache invalidation strategy
        // In a real implementation, you might want to use cache tags or more sophisticated invalidation
        if (MemoryCache != null)
        {
            Logger.LogDebug("Cache invalidation triggered for {EntityType}", typeof(TEntity).Name);
            // Note: IMemoryCache doesn't have a built-in way to remove entries by pattern
            // In a real implementation, you might use a distributed cache with tag-based invalidation
        }
    }
}

/// <summary>
/// Enhanced Entity Framework Core repository with typed key support.
/// </summary>
/// <typeparam name="TEntity">The entity type.</typeparam>
/// <typeparam name="TKey">The key type.</typeparam>
public class EnhancedEfCoreRepository<TEntity, TKey> : EnhancedEfCoreRepository<TEntity>, IRepository<TEntity, TKey> 
    where TEntity : class, IEntity<TKey>
{
    public EnhancedEfCoreRepository(
        MakingDbContext dbContext, 
        ILogger<EnhancedEfCoreRepository<TEntity, TKey>> logger,
        IMemoryCache? memoryCache = null) 
        : base(dbContext, logger, memoryCache)
    {
    }

    /// <summary>
    /// Gets multiple entities by their keys with optional caching.
    /// </summary>
    /// <param name="keys">The entity keys.</param>
    /// <param name="cacheKey">The cache key (optional).</param>
    /// <param name="cacheExpiration">The cache expiration time (optional).</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A list of entities.</returns>
    public virtual async Task<List<TEntity>> GetManyAsync(IEnumerable<TKey> keys, string? cacheKey = null, TimeSpan? cacheExpiration = null, CancellationToken cancellationToken = default)
    {
        try
        {
            // Check cache first if caching is enabled
            if (MemoryCache != null && !string.IsNullOrEmpty(cacheKey))
            {
                if (MemoryCache.TryGetValue(cacheKey, out List<TEntity>? cachedResult))
                {
                    Logger.LogDebug("Retrieved {Count} entities from cache for {EntityType}", cachedResult?.Count ?? 0, typeof(TEntity).Name);
                    return cachedResult ?? new List<TEntity>();
                }
            }

            var keyList = keys.ToList();
            var result = await Query()
                .Where(e => keyList.Contains(e.Id))
                .ToListAsync(cancellationToken);

            // Cache the result if caching is enabled
            if (MemoryCache != null && !string.IsNullOrEmpty(cacheKey))
            {
                var cacheOptions = new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = cacheExpiration ?? TimeSpan.FromMinutes(30)
                };
                MemoryCache.Set(cacheKey, result, cacheOptions);
                Logger.LogDebug("Cached {Count} entities for {EntityType} with key {CacheKey}", result.Count, typeof(TEntity).Name, cacheKey);
            }

            Logger.LogDebug("Retrieved {Count} entities by keys for {EntityType}", result.Count, typeof(TEntity).Name);
            return result;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error retrieving entities by keys for {EntityType}", typeof(TEntity).Name);
            throw;
        }
    }

    /// <summary>
    /// Gets an entity by key with optional caching.
    /// </summary>
    /// <param name="key">The entity key.</param>
    /// <param name="cacheKey">The cache key (optional).</param>
    /// <param name="cacheExpiration">The cache expiration time (optional).</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The entity or null.</returns>
    public virtual async Task<TEntity?> GetWithCacheAsync(TKey key, string? cacheKey = null, TimeSpan? cacheExpiration = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var finalCacheKey = cacheKey ?? $"{typeof(TEntity).Name}:{key}";
            
            // Check cache first if caching is enabled
            if (MemoryCache != null)
            {
                if (MemoryCache.TryGetValue(finalCacheKey, out TEntity? cachedResult))
                {
                    Logger.LogDebug("Retrieved entity from cache for {EntityType} with key {Key}", typeof(TEntity).Name, key);
                    return cachedResult;
                }
            }

            var result = await GetAsync(key);

            // Cache the result if caching is enabled
            if (MemoryCache != null && result != null)
            {
                var cacheOptions = new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = cacheExpiration ?? TimeSpan.FromMinutes(30)
                };
                MemoryCache.Set(finalCacheKey, result, cacheOptions);
                Logger.LogDebug("Cached entity for {EntityType} with key {Key}", typeof(TEntity).Name, key);
            }

            return result;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error retrieving entity with cache for {EntityType} with key {Key}", typeof(TEntity).Name, key);
            throw;
        }
    }

    /// <inheritdoc/>
    public virtual async Task<TEntity?> GetAsync(TKey id)
    {
        try
        {
            var entity = await DbSet.FindAsync(id);
            if (entity != null)
            {
                Logger.LogDebug("Found entity {EntityType} with ID: {Id}", typeof(TEntity).Name, id);
            }
            else
            {
                Logger.LogDebug("Entity {EntityType} not found with ID: {Id}", typeof(TEntity).Name, id);
            }
            return entity;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error getting entity {EntityType} with ID: {Id}", typeof(TEntity).Name, id);
            throw;
        }
    }

    /// <inheritdoc/>
    public virtual async Task DeleteAsync(TKey id)
    {
        try
        {
            var entity = await GetAsync(id);
            if (entity != null)
            {
                await DeleteAsync(entity);
                Logger.LogDebug("Deleted entity {EntityType} with ID: {Id}", typeof(TEntity).Name, id);
                
                // Invalidate specific cache entry
                var cacheKey = $"{typeof(TEntity).Name}:{id}";
                MemoryCache?.Remove(cacheKey);
            }
            else
            {
                Logger.LogDebug("Entity {EntityType} not found with ID: {Id} for deletion", typeof(TEntity).Name, id);
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error deleting entity {EntityType} with ID: {Id}", typeof(TEntity).Name, id);
            throw;
        }
    }
}