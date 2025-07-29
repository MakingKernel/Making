using Making.Ddd.Domain.Domain.Entities;
using Making.Ddd.Domain.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Linq.Expressions;

namespace Making.EntityFrameworkCore.EntityFrameworkCore;

/// <summary>
/// Entity Framework Core implementation of the repository pattern.
/// </summary>
/// <typeparam name="TEntity">The type of the entity.</typeparam>
public class EfCoreRepository<TEntity> : IRepository<TEntity> where TEntity : class, IEntity
{
    protected readonly MakingDbContext DbContext;
    protected readonly DbSet<TEntity> DbSet;
    protected readonly ILogger<EfCoreRepository<TEntity>> Logger;

    public EfCoreRepository(MakingDbContext dbContext, ILogger<EfCoreRepository<TEntity>> logger)
    {
        DbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        DbSet = dbContext.Set<TEntity>();
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public virtual async Task<TEntity?> GetAsync(params object[] keys)
    {
        try
        {
            var entity = await DbSet.FindAsync(keys);
            if (entity != null)
            {
                Logger.LogDebug("Found entity {EntityType} with keys: {Keys}", typeof(TEntity).Name, string.Join(",", keys));
            }
            else
            {
                Logger.LogDebug("Entity {EntityType} not found with keys: {Keys}", typeof(TEntity).Name, string.Join(",", keys));
            }
            return entity;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error occurred while getting entity {EntityType} with keys: {Keys}", typeof(TEntity).Name, string.Join(",", keys));
            throw;
        }
    }

    /// <inheritdoc/>
    public virtual async Task<List<TEntity>> GetListAsync(Expression<Func<TEntity, bool>>? predicate = null)
    {
        try
        {
            var query = DbSet.AsQueryable();
            
            if (predicate != null)
            {
                query = query.Where(predicate);
            }

            var entities = await query.ToListAsync();
            Logger.LogDebug("Retrieved {Count} entities of type {EntityType}", entities.Count, typeof(TEntity).Name);
            return entities;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error occurred while getting list of entities {EntityType}", typeof(TEntity).Name);
            throw;
        }
    }

    /// <inheritdoc/>
    public virtual async Task<long> GetCountAsync(Expression<Func<TEntity, bool>>? predicate = null)
    {
        try
        {
            var query = DbSet.AsQueryable();
            
            if (predicate != null)
            {
                query = query.Where(predicate);
            }

            var count = await query.LongCountAsync();
            Logger.LogDebug("Counted {Count} entities of type {EntityType}", count, typeof(TEntity).Name);
            return count;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error occurred while counting entities {EntityType}", typeof(TEntity).Name);
            throw;
        }
    }

    /// <inheritdoc/>
    public virtual async Task<TEntity> InsertAsync(TEntity entity)
    {
        try
        {
            var entityEntry = await DbSet.AddAsync(entity);
            Logger.LogDebug("Inserted entity {EntityType} with keys: {Keys}", typeof(TEntity).Name, string.Join(",", entity.GetKeys()));
            return entityEntry.Entity;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error occurred while inserting entity {EntityType}", typeof(TEntity).Name);
            throw;
        }
    }

    /// <inheritdoc/>
    public virtual Task<TEntity> UpdateAsync(TEntity entity)
    {
        try
        {
            var entityEntry = DbSet.Update(entity);
            Logger.LogDebug("Updated entity {EntityType} with keys: {Keys}", typeof(TEntity).Name, string.Join(",", entity.GetKeys()));
            return Task.FromResult(entityEntry.Entity);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error occurred while updating entity {EntityType}", typeof(TEntity).Name);
            throw;
        }
    }

    /// <inheritdoc/>
    public virtual Task DeleteAsync(TEntity entity)
    {
        try
        {
            DbSet.Remove(entity);
            Logger.LogDebug("Deleted entity {EntityType} with keys: {Keys}", typeof(TEntity).Name, string.Join(",", entity.GetKeys()));
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error occurred while deleting entity {EntityType}", typeof(TEntity).Name);
            throw;
        }
    }

    /// <inheritdoc/>
    public virtual async Task DeleteAsync(Expression<Func<TEntity, bool>> predicate)
    {
        try
        {
            var entities = await DbSet.Where(predicate).ToListAsync();
            DbSet.RemoveRange(entities);
            Logger.LogDebug("Deleted {Count} entities of type {EntityType}", entities.Count, typeof(TEntity).Name);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error occurred while bulk deleting entities {EntityType}", typeof(TEntity).Name);
            throw;
        }
    }

    /// <summary>
    /// Gets the underlying queryable for advanced scenarios.
    /// </summary>
    /// <returns>IQueryable for the entity type.</returns>
    protected virtual IQueryable<TEntity> GetQueryable()
    {
        return DbSet.AsQueryable();
    }
}

/// <summary>
/// Entity Framework Core implementation of the repository pattern for entities with typed keys.
/// </summary>
/// <typeparam name="TEntity">The type of the entity.</typeparam>
/// <typeparam name="TKey">The type of the primary key.</typeparam>
public class EfCoreRepository<TEntity, TKey> : EfCoreRepository<TEntity>, IRepository<TEntity, TKey> 
    where TEntity : class, IEntity<TKey>
{
    public EfCoreRepository(MakingDbContext dbContext, ILogger<EfCoreRepository<TEntity, TKey>> logger) 
        : base(dbContext, logger)
    {
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
            Logger.LogError(ex, "Error occurred while getting entity {EntityType} with ID: {Id}", typeof(TEntity).Name, id);
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
            }
            else
            {
                Logger.LogDebug("Entity {EntityType} not found with ID: {Id} for deletion", typeof(TEntity).Name, id);
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error occurred while deleting entity {EntityType} with ID: {Id}", typeof(TEntity).Name, id);
            throw;
        }
    }
}