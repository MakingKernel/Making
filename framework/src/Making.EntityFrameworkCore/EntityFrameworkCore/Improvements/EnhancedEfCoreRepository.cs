using Making.Ddd.Domain.Domain.Entities;
using Making.Ddd.Domain.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.Extensions.Logging;

namespace Making.EntityFrameworkCore.EntityFrameworkCore.Improvements;

/// <summary>
/// Enhanced EF Core Repository with advanced features like automatic soft delete filtering,
/// pagination, specification pattern support, and high-performance bulk operations
/// </summary>
public class EnhancedEfCoreRepository<TEntity> : IRepository<TEntity>
    where TEntity : class, IEntity
{
    protected readonly MakingDbContext DbContext;
    protected readonly DbSet<TEntity> DbSet;
    protected readonly ILogger<EnhancedEfCoreRepository<TEntity>> Logger;

    public EnhancedEfCoreRepository(MakingDbContext dbContext, ILogger<EnhancedEfCoreRepository<TEntity>> logger)
    {
        DbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        DbSet = dbContext.Set<TEntity>();
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    #region Query Operations

    /// <summary>
    /// Get queryable with automatic soft delete filtering
    /// </summary>
    /// <param name="includeSoftDeleted">Whether to include soft deleted entities</param>
    /// <returns>IQueryable for the entity</returns>
    public virtual IQueryable<TEntity> GetQueryable(bool includeSoftDeleted = false)
    {
        var query = DbSet.AsQueryable();

        // Apply soft delete filter if entity supports it and not explicitly including soft deleted
        if (!includeSoftDeleted && typeof(ISoftDelete).IsAssignableFrom(typeof(TEntity)))
        {
            query = query.Where(e => !((ISoftDelete)e).IsDeleted);
        }

        return query;
    }

    /// <summary>
    /// Get all entities with optional filtering
    /// </summary>
    public virtual async Task<IEnumerable<TEntity>> GetAllAsync(
        Expression<Func<TEntity, bool>>? predicate = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        bool includeSoftDeleted = false,
        CancellationToken cancellationToken = default)
    {
        var query = GetQueryable(includeSoftDeleted);

        if (predicate != null)
        {
            query = query.Where(predicate);
        }

        if (orderBy != null)
        {
            query = orderBy(query);
        }

        return await query.ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Get paginated results with comprehensive pagination support
    /// </summary>
    public virtual async Task<PagedResult<TEntity>> GetPagedAsync(
        int pageNumber,
        int pageSize,
        Expression<Func<TEntity, bool>>? predicate = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        bool includeSoftDeleted = false,
        CancellationToken cancellationToken = default)
    {
        if (pageNumber < 1) pageNumber = 1;
        if (pageSize < 1) pageSize = 10;

        var query = GetQueryable(includeSoftDeleted);

        if (predicate != null)
        {
            query = query.Where(predicate);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        if (orderBy != null)
        {
            query = orderBy(query);
        }

        var items = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<TEntity>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalPages = (int)Math.Ceiling((double)totalCount / pageSize)
        };
    }

    /// <summary>
    /// Find entity by predicate
    /// </summary>
    public virtual async Task<TEntity?> FindAsync(
        Expression<Func<TEntity, bool>> predicate,
        bool includeSoftDeleted = false,
        CancellationToken cancellationToken = default)
    {
        var query = GetQueryable(includeSoftDeleted);
        return await query.FirstOrDefaultAsync(predicate, cancellationToken);
    }

    /// <summary>
    /// Get entity by id
    /// </summary>
    public virtual async Task<TEntity?> GetByIdAsync(object id, CancellationToken cancellationToken = default)
    {
        return await DbSet.FindAsync(new[] { id }, cancellationToken);
    }

    /// <summary>
    /// Check if entity exists
    /// </summary>
    public virtual async Task<bool> ExistsAsync(
        Expression<Func<TEntity, bool>> predicate,
        bool includeSoftDeleted = false,
        CancellationToken cancellationToken = default)
    {
        var query = GetQueryable(includeSoftDeleted);
        return await query.AnyAsync(predicate, cancellationToken);
    }

    /// <summary>
    /// Count entities
    /// </summary>
    public virtual async Task<int> CountAsync(
        Expression<Func<TEntity, bool>>? predicate = null,
        bool includeSoftDeleted = false,
        CancellationToken cancellationToken = default)
    {
        var query = GetQueryable(includeSoftDeleted);

        if (predicate != null)
        {
            query = query.Where(predicate);
        }

        return await query.CountAsync(cancellationToken);
    }

    #endregion

    #region Modification Operations

    /// <summary>
    /// Insert single entity
    /// </summary>
    public virtual async Task<TEntity> InsertAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        if (entity == null) throw new ArgumentNullException(nameof(entity));

        await DbSet.AddAsync(entity, cancellationToken);
        Logger.LogDebug("Added entity {EntityType} to context", typeof(TEntity).Name);
        return entity;
    }

    /// <summary>
    /// Insert multiple entities with high performance
    /// </summary>
    public virtual async Task<IEnumerable<TEntity>> InsertRangeAsync(
        IEnumerable<TEntity> entities,
        CancellationToken cancellationToken = default)
    {
        if (entities == null) throw new ArgumentNullException(nameof(entities));

        var entityList = entities.ToList();
        if (entityList.Count == 0) return entityList;

        await DbSet.AddRangeAsync(entityList, cancellationToken);
        Logger.LogDebug("Added {Count} entities of type {EntityType} to context", entityList.Count,
            typeof(TEntity).Name);
        return entityList;
    }

    /// <summary>
    /// Update entity
    /// </summary>
    public virtual TEntity Update(TEntity entity)
    {
        if (entity == null) throw new ArgumentNullException(nameof(entity));

        DbSet.Update(entity);
        Logger.LogDebug("Updated entity {EntityType}", typeof(TEntity).Name);
        return entity;
    }

    /// <summary>
    /// Update multiple entities
    /// </summary>
    public virtual IEnumerable<TEntity> UpdateRange(IEnumerable<TEntity> entities)
    {
        if (entities == null) throw new ArgumentNullException(nameof(entities));

        var entityList = entities.ToList();
        if (entityList.Count == 0) return entityList;

        DbSet.UpdateRange(entityList);
        Logger.LogDebug("Updated {Count} entities of type {EntityType}", entityList.Count, typeof(TEntity).Name);
        return entityList;
    }

    /// <summary>
    /// Delete entity (supports soft delete)
    /// </summary>
    public virtual void Delete(TEntity entity)
    {
        if (entity == null) throw new ArgumentNullException(nameof(entity));

        // Check if entity supports soft delete
        if (entity is ISoftDelete softDeleteEntity)
        {
            softDeleteEntity.IsDeleted = true;
            softDeleteEntity.DeletionTime = DateTime.UtcNow;
            DbSet.Update(entity);
            Logger.LogDebug("Soft deleted entity {EntityType}", typeof(TEntity).Name);
        }
        else
        {
            DbSet.Remove(entity);
            Logger.LogDebug("Hard deleted entity {EntityType}", typeof(TEntity).Name);
        }
    }

    /// <summary>
    /// Delete entity by id
    /// </summary>
    public virtual async Task DeleteAsync(object id, CancellationToken cancellationToken = default)
    {
        var entity = await GetByIdAsync(id, cancellationToken);
        if (entity != null)
        {
            Delete(entity);
        }
    }

    /// <summary>
    /// Delete multiple entities
    /// </summary>
    public virtual void DeleteRange(IEnumerable<TEntity> entities)
    {
        if (entities == null) throw new ArgumentNullException(nameof(entities));

        var entityList = entities.ToList();
        if (entityList.Count == 0) return;

        // Check if entities support soft delete
        var softDeleteEntities = entityList.OfType<ISoftDelete>().ToList();
        if (softDeleteEntities.Count == entityList.Count)
        {
            // All entities support soft delete
            var now = DateTime.UtcNow;
            foreach (var entity in softDeleteEntities)
            {
                entity.IsDeleted = true;
                entity.DeletionTime = now;
            }

            DbSet.UpdateRange(entityList.Cast<TEntity>());
            Logger.LogDebug("Soft deleted {Count} entities of type {EntityType}", entityList.Count,
                typeof(TEntity).Name);
        }
        else
        {
            // Hard delete
            DbSet.RemoveRange(entityList);
            Logger.LogDebug("Hard deleted {Count} entities of type {EntityType}", entityList.Count,
                typeof(TEntity).Name);
        }
    }

    /// <summary>
    /// Permanently delete entity (bypasses soft delete)
    /// </summary>
    public virtual void HardDelete(TEntity entity)
    {
        if (entity == null) throw new ArgumentNullException(nameof(entity));

        DbSet.Remove(entity);
        Logger.LogDebug("Hard deleted entity {EntityType}", typeof(TEntity).Name);
    }

    #endregion

    #region Bulk Operations (EF Core 7+ features)

    /// <summary>
    /// High-performance bulk update using ExecuteUpdate
    /// </summary>
    public virtual async Task<int> BulkUpdateAsync(
        Expression<Func<TEntity, bool>> predicate,
        Expression<Func<SetPropertyCalls<TEntity>, SetPropertyCalls<TEntity>>> setPropertyCalls,
        bool includeSoftDeleted = false,
        CancellationToken cancellationToken = default)
    {
        var query = GetQueryable(includeSoftDeleted).Where(predicate);
        var result = await query.ExecuteUpdateAsync(setPropertyCalls, cancellationToken);
        Logger.LogDebug("Bulk updated {Count} entities of type {EntityType}", result, typeof(TEntity).Name);
        return result;
    }

    /// <summary>
    /// High-performance bulk delete using ExecuteDelete
    /// </summary>
    public virtual async Task<int> BulkDeleteAsync(
        Expression<Func<TEntity, bool>> predicate,
        bool includeSoftDeleted = false,
        bool forceHardDelete = false,
        CancellationToken cancellationToken = default)
    {
        var query = GetQueryable(includeSoftDeleted).Where(predicate);

        // Check if should use soft delete
        if (!forceHardDelete && typeof(ISoftDelete).IsAssignableFrom(typeof(TEntity)))
        {
            // Use bulk update to set IsDeleted = true
            var result = await query.ExecuteUpdateAsync(
                calls => calls
                    .SetProperty(e => ((ISoftDelete)e).IsDeleted, true)
                    .SetProperty(e => ((ISoftDelete)e).DeletionTime, DateTime.UtcNow),
                cancellationToken);
            Logger.LogDebug("Bulk soft deleted {Count} entities of type {EntityType}", result, typeof(TEntity).Name);
            return result;
        }
        else
        {
            // Hard delete
            var result = await query.ExecuteDeleteAsync(cancellationToken);
            Logger.LogDebug("Bulk hard deleted {Count} entities of type {EntityType}", result, typeof(TEntity).Name);
            return result;
        }
    }

    #endregion

    #region Specification Pattern Support

    /// <summary>
    /// Apply specification to query
    /// </summary>
    public virtual IQueryable<TEntity> ApplySpecification<TSpec>(TSpec specification)
        where TSpec : ISpecification<TEntity>
    {
        var query = GetQueryable(specification.IncludeSoftDeleted);

        if (specification.Predicate != null)
        {
            query = query.Where(specification.Predicate);
        }

        if (specification.OrderBy != null)
        {
            query = specification.OrderBy(query);
        }

        if (specification.Skip.HasValue)
        {
            query = query.Skip(specification.Skip.Value);
        }

        if (specification.Take.HasValue)
        {
            query = query.Take(specification.Take.Value);
        }

        return query;
    }

    /// <summary>
    /// Get entities by specification
    /// </summary>
    public virtual async Task<IEnumerable<TEntity>> GetBySpecificationAsync<TSpec>(
        TSpec specification,
        CancellationToken cancellationToken = default)
        where TSpec : ISpecification<TEntity>
    {
        var query = ApplySpecification(specification);
        return await query.ToListAsync(cancellationToken);
    }

    #endregion

    #region IRepository Interface Implementation

    public async Task<TEntity?> GetAsync(params object[] keys)
    {
        if (keys == null || keys.Length == 0)
            return null;

        return await DbSet.FindAsync(keys);
    }

    public async Task<List<TEntity>> GetListAsync(Expression<Func<TEntity, bool>>? predicate = null)
    {
        var query = GetQueryable(false);

        if (predicate != null)
        {
            query = query.Where(predicate);
        }

        return await query.ToListAsync();
    }

    public async Task<long> GetCountAsync(Expression<Func<TEntity, bool>>? predicate = null)
    {
        var query = GetQueryable(false);

        if (predicate != null)
        {
            query = query.Where(predicate);
        }

        return await query.LongCountAsync();
    }

    public async Task<TEntity> InsertAsync(TEntity entity)
    {
        if (entity == null) throw new ArgumentNullException(nameof(entity));

        await DbSet.AddAsync(entity);
        await DbContext.SaveChangesAsync();
        Logger.LogDebug("Inserted entity {EntityType} with auto-save", typeof(TEntity).Name);
        return entity;
    }

    public async Task<TEntity> UpdateAsync(TEntity entity)
    {
        if (entity == null) throw new ArgumentNullException(nameof(entity));

        DbSet.Update(entity);
        await DbContext.SaveChangesAsync();
        Logger.LogDebug("Updated entity {EntityType} with auto-save", typeof(TEntity).Name);
        return entity;
    }

    public async Task DeleteAsync(TEntity entity)
    {
        if (entity == null) throw new ArgumentNullException(nameof(entity));

        // Check if entity supports soft delete
        if (entity is ISoftDelete softDeleteEntity)
        {
            softDeleteEntity.IsDeleted = true;
            softDeleteEntity.DeletionTime = DateTime.UtcNow;
            DbSet.Update(entity);
            Logger.LogDebug("Soft deleted entity {EntityType} with auto-save", typeof(TEntity).Name);
        }
        else
        {
            DbSet.Remove(entity);
            Logger.LogDebug("Hard deleted entity {EntityType} with auto-save", typeof(TEntity).Name);
        }

        await DbContext.SaveChangesAsync();
    }

    public async Task DeleteAsync(Expression<Func<TEntity, bool>> predicate)
    {
        if (predicate == null) throw new ArgumentNullException(nameof(predicate));

        // Check if entity supports soft delete
        if (typeof(ISoftDelete).IsAssignableFrom(typeof(TEntity)))
        {
            // Use bulk update for soft delete
            var query = GetQueryable(false).Where(predicate);
            var result = await query.ExecuteUpdateAsync(
                calls => calls
                    .SetProperty(e => ((ISoftDelete)e).IsDeleted, true)
                    .SetProperty(e => ((ISoftDelete)e).DeletionTime, DateTime.UtcNow));
            Logger.LogDebug("Bulk soft deleted {Count} entities of type {EntityType} with predicate", result, typeof(TEntity).Name);
        }
        else
        {
            // Use bulk delete for hard delete
            var query = GetQueryable(false).Where(predicate);
            var result = await query.ExecuteDeleteAsync();
            Logger.LogDebug("Bulk hard deleted {Count} entities of type {EntityType} with predicate", result, typeof(TEntity).Name);
        }
    }

    #endregion
}

/// <summary>
/// Enhanced EF Core Repository with typed key support
/// </summary>
public class EnhancedEfCoreRepository<TEntity, TKey> : EnhancedEfCoreRepository<TEntity>, IRepository<TEntity, TKey>
    where TEntity : class, IEntity<TKey>
{
    public EnhancedEfCoreRepository(MakingDbContext dbContext, ILogger<EnhancedEfCoreRepository<TEntity, TKey>> logger)
        : base(dbContext, logger)
    {
    }

    /// <summary>
    /// Get entity by typed key
    /// </summary>
    public virtual async Task<TEntity?> GetByIdAsync(TKey id, CancellationToken cancellationToken = default)
    {
        return await DbSet.FindAsync(new object[] { id! }, cancellationToken);
    }

    /// <summary>
    /// Delete entity by typed key
    /// </summary>
    public virtual async Task DeleteAsync(TKey id, CancellationToken cancellationToken = default)
    {
        var entity = await GetByIdAsync(id, cancellationToken);
        if (entity != null)
        {
            Delete(entity);
        }
    }

    public async Task<TEntity?> GetAsync(TKey id)
    {
        return await DbSet.Where(x => x.Id.Equals(id)).FirstOrDefaultAsync();
    }

    public async Task DeleteAsync(TKey id)
    {
        await DbSet.Where(x => x.Id.Equals(id)).ExecuteDeleteAsync();
    }
}

/// <summary>
/// Pagination result wrapper
/// </summary>
public class PagedResult<T>
{
    public IEnumerable<T> Items { get; set; } = Enumerable.Empty<T>();
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
    public bool HasPreviousPage => PageNumber > 1;
    public bool HasNextPage => PageNumber < TotalPages;
}

/// <summary>
/// Specification pattern interface
/// </summary>
public interface ISpecification<T>
{
    Expression<Func<T, bool>>? Predicate { get; }
    Func<IQueryable<T>, IOrderedQueryable<T>>? OrderBy { get; }
    int? Skip { get; }
    int? Take { get; }
    bool IncludeSoftDeleted { get; }
}