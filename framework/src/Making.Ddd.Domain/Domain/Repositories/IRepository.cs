using Making.Ddd.Domain.Domain.Entities;
using System.Linq.Expressions;

namespace Making.Ddd.Domain.Domain.Repositories;

/// <summary>
/// Base interface for repositories.
/// </summary>
/// <typeparam name="TEntity">The type of the entity.</typeparam>
public interface IRepository<TEntity> where TEntity : class, IEntity
{
    /// <summary>
    /// Gets an entity by its keys.
    /// </summary>
    /// <param name="keys">The keys of the entity.</param>
    /// <returns>The entity if found, null otherwise.</returns>
    Task<TEntity?> GetAsync(params object[] keys);

    /// <summary>
    /// Gets a list of entities based on the given predicate.
    /// </summary>
    /// <param name="predicate">The predicate to filter entities.</param>
    /// <returns>A list of entities.</returns>
    Task<List<TEntity>> GetListAsync(Expression<Func<TEntity, bool>>? predicate = null);

    /// <summary>
    /// Gets the count of entities based on the given predicate.
    /// </summary>
    /// <param name="predicate">The predicate to filter entities.</param>
    /// <returns>The count of entities.</returns>
    Task<long> GetCountAsync(Expression<Func<TEntity, bool>>? predicate = null);

    /// <summary>
    /// Inserts a new entity.
    /// </summary>
    /// <param name="entity">The entity to insert.</param>
    /// <returns>The inserted entity.</returns>
    Task<TEntity> InsertAsync(TEntity entity);

    /// <summary>
    /// Updates an existing entity.
    /// </summary>
    /// <param name="entity">The entity to update.</param>
    /// <returns>The updated entity.</returns>
    Task<TEntity> UpdateAsync(TEntity entity);

    /// <summary>
    /// Deletes an entity.
    /// </summary>
    /// <param name="entity">The entity to delete.</param>
    Task DeleteAsync(TEntity entity);

    /// <summary>
    /// Deletes entities based on the given predicate.
    /// </summary>
    /// <param name="predicate">The predicate to filter entities to delete.</param>
    Task DeleteAsync(Expression<Func<TEntity, bool>> predicate);
}

/// <summary>
/// Repository interface for entities with a typed key.
/// </summary>
/// <typeparam name="TEntity">The type of the entity.</typeparam>
/// <typeparam name="TKey">The type of the primary key.</typeparam>
public interface IRepository<TEntity, TKey> : IRepository<TEntity> 
    where TEntity : class, IEntity<TKey>
{
    /// <summary>
    /// Gets an entity by its identifier.
    /// </summary>
    /// <param name="id">The identifier of the entity.</param>
    /// <returns>The entity if found, null otherwise.</returns>
    Task<TEntity?> GetAsync(TKey id);

    /// <summary>
    /// Deletes an entity by its identifier.
    /// </summary>
    /// <param name="id">The identifier of the entity to delete.</param>
    Task DeleteAsync(TKey id);
}