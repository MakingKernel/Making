using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Linq.Expressions;

namespace Making.EntityFrameworkCore.Query;

/// <summary>
/// Entity Framework Core implementation of the query builder pattern.
/// </summary>
/// <typeparam name="T">The entity type.</typeparam>
public class EfCoreQueryBuilder<T> : IQueryBuilder<T> where T : class
{
    private readonly IQueryable<T> _query;
    private readonly ILogger<EfCoreQueryBuilder<T>> _logger;

    /// <summary>
    /// Initializes a new instance of the EfCoreQueryBuilder class.
    /// </summary>
    /// <param name="query">The initial queryable.</param>
    /// <param name="logger">The logger instance.</param>
    public EfCoreQueryBuilder(IQueryable<T> query, ILogger<EfCoreQueryBuilder<T>> logger)
    {
        _query = query ?? throw new ArgumentNullException(nameof(query));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public IQueryBuilder<T> Where(Expression<Func<T, bool>> predicate)
    {
        try
        {
            var newQuery = _query.Where(predicate);
            _logger.LogDebug("Applied WHERE condition to query for {EntityType}", typeof(T).Name);
            return new EfCoreQueryBuilder<T>(newQuery, _logger);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error applying WHERE condition to query for {EntityType}", typeof(T).Name);
            throw;
        }
    }

    /// <inheritdoc/>
    public IQueryBuilder<T> Include<TProperty>(Expression<Func<T, TProperty>> navigationProperty)
    {
        try
        {
            var newQuery = _query.Include(navigationProperty);
            _logger.LogDebug("Applied INCLUDE to query for {EntityType}", typeof(T).Name);
            return new EfCoreQueryBuilder<T>(newQuery, _logger);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error applying INCLUDE to query for {EntityType}", typeof(T).Name);
            throw;
        }
    }

    /// <inheritdoc/>
    public IQueryBuilder<T> Include(string navigationProperty)
    {
        try
        {
            var newQuery = _query.Include(navigationProperty);
            _logger.LogDebug("Applied INCLUDE ({NavigationProperty}) to query for {EntityType}", 
                navigationProperty, typeof(T).Name);
            return new EfCoreQueryBuilder<T>(newQuery, _logger);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error applying INCLUDE ({NavigationProperty}) to query for {EntityType}", 
                navigationProperty, typeof(T).Name);
            throw;
        }
    }

    /// <inheritdoc/>
    public IQueryBuilder<T> IncludeThen<TProperty, TThenProperty>(
        Expression<Func<T, TProperty>> navigationProperty,
        Expression<Func<TProperty, TThenProperty>> thenNavigationProperty)
    {
        try
        {
            var newQuery = _query.Include(navigationProperty).ThenInclude(thenNavigationProperty);
            _logger.LogDebug("Applied INCLUDE-THEN to query for {EntityType}", typeof(T).Name);
            return new EfCoreQueryBuilder<T>(newQuery, _logger);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error applying INCLUDE-THEN to query for {EntityType}", typeof(T).Name);
            throw;
        }
    }

    /// <inheritdoc/>
    public IQueryBuilder<T> OrderBy<TKey>(Expression<Func<T, TKey>> keySelector)
    {
        try
        {
            var newQuery = _query.OrderBy(keySelector);
            _logger.LogDebug("Applied ORDER BY to query for {EntityType}", typeof(T).Name);
            return new EfCoreQueryBuilder<T>(newQuery, _logger);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error applying ORDER BY to query for {EntityType}", typeof(T).Name);
            throw;
        }
    }

    /// <inheritdoc/>
    public IQueryBuilder<T> OrderByDescending<TKey>(Expression<Func<T, TKey>> keySelector)
    {
        try
        {
            var newQuery = _query.OrderByDescending(keySelector);
            _logger.LogDebug("Applied ORDER BY DESC to query for {EntityType}", typeof(T).Name);
            return new EfCoreQueryBuilder<T>(newQuery, _logger);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error applying ORDER BY DESC to query for {EntityType}", typeof(T).Name);
            throw;
        }
    }

    /// <inheritdoc/>
    public IQueryBuilder<T> ThenBy<TKey>(Expression<Func<T, TKey>> keySelector)
    {
        try
        {
            if (_query is IOrderedQueryable<T> orderedQuery)
            {
                var newQuery = orderedQuery.ThenBy(keySelector);
                _logger.LogDebug("Applied THEN BY to query for {EntityType}", typeof(T).Name);
                return new EfCoreQueryBuilder<T>(newQuery, _logger);
            }

            _logger.LogWarning("THEN BY applied to non-ordered query for {EntityType}, applying ORDER BY instead", typeof(T).Name);
            return OrderBy(keySelector);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error applying THEN BY to query for {EntityType}", typeof(T).Name);
            throw;
        }
    }

    /// <inheritdoc/>
    public IQueryBuilder<T> ThenByDescending<TKey>(Expression<Func<T, TKey>> keySelector)
    {
        try
        {
            if (_query is IOrderedQueryable<T> orderedQuery)
            {
                var newQuery = orderedQuery.ThenByDescending(keySelector);
                _logger.LogDebug("Applied THEN BY DESC to query for {EntityType}", typeof(T).Name);
                return new EfCoreQueryBuilder<T>(newQuery, _logger);
            }

            _logger.LogWarning("THEN BY DESC applied to non-ordered query for {EntityType}, applying ORDER BY DESC instead", typeof(T).Name);
            return OrderByDescending(keySelector);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error applying THEN BY DESC to query for {EntityType}", typeof(T).Name);
            throw;
        }
    }

    /// <inheritdoc/>
    public IQueryBuilder<T> GroupBy<TKey>(Expression<Func<T, TKey>> keySelector)
    {
        try
        {
            var newQuery = _query.GroupBy(keySelector).SelectMany(g => g);
            _logger.LogDebug("Applied GROUP BY to query for {EntityType}", typeof(T).Name);
            return new EfCoreQueryBuilder<T>(newQuery, _logger);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error applying GROUP BY to query for {EntityType}", typeof(T).Name);
            throw;
        }
    }

    /// <inheritdoc/>
    public IQueryBuilder<T> Skip(int count)
    {
        try
        {
            var newQuery = _query.Skip(count);
            _logger.LogDebug("Applied SKIP ({Count}) to query for {EntityType}", count, typeof(T).Name);
            return new EfCoreQueryBuilder<T>(newQuery, _logger);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error applying SKIP to query for {EntityType}", typeof(T).Name);
            throw;
        }
    }

    /// <inheritdoc/>
    public IQueryBuilder<T> Take(int count)
    {
        try
        {
            var newQuery = _query.Take(count);
            _logger.LogDebug("Applied TAKE ({Count}) to query for {EntityType}", count, typeof(T).Name);
            return new EfCoreQueryBuilder<T>(newQuery, _logger);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error applying TAKE to query for {EntityType}", typeof(T).Name);
            throw;
        }
    }

    /// <inheritdoc/>
    public IQueryBuilder<T> AsNoTracking()
    {
        try
        {
            var newQuery = _query.AsNoTracking();
            _logger.LogDebug("Applied AS NO TRACKING to query for {EntityType}", typeof(T).Name);
            return new EfCoreQueryBuilder<T>(newQuery, _logger);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error applying AS NO TRACKING to query for {EntityType}", typeof(T).Name);
            throw;
        }
    }

    /// <inheritdoc/>
    public IQueryBuilder<T> AsSplitQuery()
    {
        try
        {
            var newQuery = _query.AsSplitQuery();
            _logger.LogDebug("Applied AS SPLIT QUERY to query for {EntityType}", typeof(T).Name);
            return new EfCoreQueryBuilder<T>(newQuery, _logger);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error applying AS SPLIT QUERY to query for {EntityType}", typeof(T).Name);
            throw;
        }
    }

    /// <inheritdoc/>
    public IQueryBuilder<T> ApplySpecification(ISpecification<T> specification)
    {
        try
        {
            var query = _query;

            // Apply criteria
            if (specification.Criteria != null)
            {
                query = query.Where(specification.Criteria);
            }

            // Apply includes
            query = specification.Includes.Aggregate(query, (current, include) => current.Include(include));
            query = specification.IncludeStrings.Aggregate(query, (current, include) => current.Include(include));

            // Apply ordering
            if (specification.OrderBy != null)
            {
                query = specification.OrderBy(query);
            }

            // Apply grouping
            if (specification.GroupBy != null)
            {
                query = query.GroupBy(specification.GroupBy).SelectMany(g => g);
            }

            // Apply pagination
            if (specification.Skip.HasValue)
            {
                query = query.Skip(specification.Skip.Value);
            }

            if (specification.Take.HasValue)
            {
                query = query.Take(specification.Take.Value);
            }

            // Apply performance options
            if (specification.AsNoTracking)
            {
                query = query.AsNoTracking();
            }

            if (specification.AsSplitQuery)
            {
                query = query.AsSplitQuery();
            }

            _logger.LogDebug("Applied specification to query for {EntityType}", typeof(T).Name);
            return new EfCoreQueryBuilder<T>(query, _logger);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error applying specification to query for {EntityType}", typeof(T).Name);
            throw;
        }
    }

    /// <inheritdoc/>
    public IQueryBuilder<TResult> Select<TResult>(Expression<Func<T, TResult>> selector) where TResult : class
    {
        try
        {
            var newQuery = _query.Select(selector);
            _logger.LogDebug("Applied SELECT projection from {SourceType} to {ResultType}", 
                typeof(T).Name, typeof(TResult).Name);
            return new EfCoreQueryBuilder<TResult>(newQuery, 
                _logger as ILogger<EfCoreQueryBuilder<TResult>> ?? 
                throw new InvalidOperationException("Logger type mismatch"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error applying SELECT projection from {SourceType} to {ResultType}", 
                typeof(T).Name, typeof(TResult).Name);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<List<T>> ToListAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _query.ToListAsync(cancellationToken);
            _logger.LogDebug("Executed query for {EntityType}, returned {Count} items", 
                typeof(T).Name, result.Count);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing ToListAsync for {EntityType}", typeof(T).Name);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<T?> FirstOrDefaultAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _query.FirstOrDefaultAsync(cancellationToken);
            _logger.LogDebug("Executed FirstOrDefaultAsync for {EntityType}, found: {Found}", 
                typeof(T).Name, result != null);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing FirstOrDefaultAsync for {EntityType}", typeof(T).Name);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<T?> SingleOrDefaultAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _query.SingleOrDefaultAsync(cancellationToken);
            _logger.LogDebug("Executed SingleOrDefaultAsync for {EntityType}, found: {Found}", 
                typeof(T).Name, result != null);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing SingleOrDefaultAsync for {EntityType}", typeof(T).Name);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<int> CountAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _query.CountAsync(cancellationToken);
            _logger.LogDebug("Executed CountAsync for {EntityType}, count: {Count}", 
                typeof(T).Name, result);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing CountAsync for {EntityType}", typeof(T).Name);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<long> LongCountAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _query.LongCountAsync(cancellationToken);
            _logger.LogDebug("Executed LongCountAsync for {EntityType}, count: {Count}", 
                typeof(T).Name, result);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing LongCountAsync for {EntityType}", typeof(T).Name);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<bool> AnyAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _query.AnyAsync(cancellationToken);
            _logger.LogDebug("Executed AnyAsync for {EntityType}, result: {Result}", 
                typeof(T).Name, result);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing AnyAsync for {EntityType}", typeof(T).Name);
            throw;
        }
    }

    /// <inheritdoc/>
    public IQueryable<T> AsQueryable()
    {
        return _query;
    }
}