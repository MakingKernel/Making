using System.Linq.Expressions;

namespace Making.EntityFrameworkCore.Query;

/// <summary>
/// Provides a fluent API for building complex queries with method chaining.
/// </summary>
/// <typeparam name="T">The entity type.</typeparam>
public interface IQueryBuilder<T> where T : class
{
    /// <summary>
    /// Adds a where condition to the query.
    /// </summary>
    /// <param name="predicate">The filter predicate.</param>
    /// <returns>The query builder for method chaining.</returns>
    IQueryBuilder<T> Where(Expression<Func<T, bool>> predicate);

    /// <summary>
    /// Includes a navigation property in the query.
    /// </summary>
    /// <typeparam name="TProperty">The property type.</typeparam>
    /// <param name="navigationProperty">The navigation property expression.</param>
    /// <returns>The query builder for method chaining.</returns>
    IQueryBuilder<T> Include<TProperty>(Expression<Func<T, TProperty>> navigationProperty);

    /// <summary>
    /// Includes a navigation property using string notation.
    /// </summary>
    /// <param name="navigationProperty">The navigation property name.</param>
    /// <returns>The query builder for method chaining.</returns>
    IQueryBuilder<T> Include(string navigationProperty);

    /// <summary>
    /// Includes related data and then includes a nested navigation property.
    /// </summary>
    /// <typeparam name="TProperty">The property type.</typeparam>
    /// <typeparam name="TThenProperty">The nested property type.</typeparam>
    /// <param name="navigationProperty">The navigation property expression.</param>
    /// <param name="thenNavigationProperty">The nested navigation property expression.</param>
    /// <returns>The query builder for method chaining.</returns>
    IQueryBuilder<T> IncludeThen<TProperty, TThenProperty>(
        Expression<Func<T, TProperty>> navigationProperty,
        Expression<Func<TProperty, TThenProperty>> thenNavigationProperty);

    /// <summary>
    /// Orders the query results by the specified key in ascending order.
    /// </summary>
    /// <typeparam name="TKey">The key type.</typeparam>
    /// <param name="keySelector">The key selector expression.</param>
    /// <returns>The query builder for method chaining.</returns>
    IQueryBuilder<T> OrderBy<TKey>(Expression<Func<T, TKey>> keySelector);

    /// <summary>
    /// Orders the query results by the specified key in descending order.
    /// </summary>
    /// <typeparam name="TKey">The key type.</typeparam>
    /// <param name="keySelector">The key selector expression.</param>
    /// <returns>The query builder for method chaining.</returns>
    IQueryBuilder<T> OrderByDescending<TKey>(Expression<Func<T, TKey>> keySelector);

    /// <summary>
    /// Adds a secondary ordering to the query in ascending order.
    /// </summary>
    /// <typeparam name="TKey">The key type.</typeparam>
    /// <param name="keySelector">The key selector expression.</param>
    /// <returns>The query builder for method chaining.</returns>
    IQueryBuilder<T> ThenBy<TKey>(Expression<Func<T, TKey>> keySelector);

    /// <summary>
    /// Adds a secondary ordering to the query in descending order.
    /// </summary>
    /// <typeparam name="TKey">The key type.</typeparam>
    /// <param name="keySelector">The key selector expression.</param>
    /// <returns>The query builder for method chaining.</returns>
    IQueryBuilder<T> ThenByDescending<TKey>(Expression<Func<T, TKey>> keySelector);

    /// <summary>
    /// Groups the query results by the specified key.
    /// </summary>
    /// <typeparam name="TKey">The key type.</typeparam>
    /// <param name="keySelector">The key selector expression.</param>
    /// <returns>The query builder for method chaining.</returns>
    IQueryBuilder<T> GroupBy<TKey>(Expression<Func<T, TKey>> keySelector);

    /// <summary>
    /// Skips the specified number of elements.
    /// </summary>
    /// <param name="count">The number of elements to skip.</param>
    /// <returns>The query builder for method chaining.</returns>
    IQueryBuilder<T> Skip(int count);

    /// <summary>
    /// Takes the specified number of elements.
    /// </summary>
    /// <param name="count">The number of elements to take.</param>
    /// <returns>The query builder for method chaining.</returns>
    IQueryBuilder<T> Take(int count);

    /// <summary>
    /// Configures the query to use no change tracking.
    /// </summary>
    /// <returns>The query builder for method chaining.</returns>
    IQueryBuilder<T> AsNoTracking();

    /// <summary>
    /// Configures the query to use split queries for complex includes.
    /// </summary>
    /// <returns>The query builder for method chaining.</returns>
    IQueryBuilder<T> AsSplitQuery();

    /// <summary>
    /// Applies a specification to the query.
    /// </summary>
    /// <param name="specification">The specification to apply.</param>
    /// <returns>The query builder for method chaining.</returns>
    IQueryBuilder<T> ApplySpecification(ISpecification<T> specification);

    /// <summary>
    /// Projects the query results to a different type.
    /// </summary>
    /// <typeparam name="TResult">The result type.</typeparam>
    /// <param name="selector">The projection selector.</param>
    /// <returns>A new query builder for the projected type.</returns>
    IQueryBuilder<TResult> Select<TResult>(Expression<Func<T, TResult>> selector) where TResult : class;

    /// <summary>
    /// Executes the query and returns all matching entities.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A list of matching entities.</returns>
    Task<List<T>> ToListAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes the query and returns the first matching entity or null.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The first matching entity or null.</returns>
    Task<T?> FirstOrDefaultAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes the query and returns the single matching entity or null.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The single matching entity or null.</returns>
    Task<T?> SingleOrDefaultAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes the query and returns the count of matching entities.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The count of matching entities.</returns>
    Task<int> CountAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes the query and returns the long count of matching entities.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The long count of matching entities.</returns>
    Task<long> LongCountAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if any entities match the query.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>True if any entities match; otherwise, false.</returns>
    Task<bool> AnyAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the underlying queryable for advanced scenarios.
    /// </summary>
    /// <returns>The underlying queryable.</returns>
    IQueryable<T> AsQueryable();
}

/// <summary>
/// Provides pagination result with total count information.
/// </summary>
/// <typeparam name="T">The entity type.</typeparam>
public class PagedResult<T>
{
    public List<T> Items { get; set; } = new();
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public long TotalCount { get; set; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
    public bool HasPreviousPage => PageNumber > 1;
    public bool HasNextPage => PageNumber < TotalPages;
}

/// <summary>
/// Extensions for query builder pagination.
/// </summary>
public static class QueryBuilderExtensions
{
    /// <summary>
    /// Executes the query with pagination and returns a paged result.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="queryBuilder">The query builder.</param>
    /// <param name="pageNumber">The page number (1-based).</param>
    /// <param name="pageSize">The page size.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A paged result containing the entities and pagination metadata.</returns>
    public static async Task<PagedResult<T>> ToPagedListAsync<T>(
        this IQueryBuilder<T> queryBuilder,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default) where T : class
    {
        if (pageNumber < 1) pageNumber = 1;
        if (pageSize < 1) pageSize = 10;

        var totalCount = await queryBuilder.LongCountAsync(cancellationToken);
        var items = await queryBuilder
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<T>
        {
            Items = items,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    /// <summary>
    /// Applies conditional filtering to the query.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="queryBuilder">The query builder.</param>
    /// <param name="condition">The condition to check.</param>
    /// <param name="predicate">The filter predicate to apply if condition is true.</param>
    /// <returns>The query builder for method chaining.</returns>
    public static IQueryBuilder<T> WhereIf<T>(
        this IQueryBuilder<T> queryBuilder,
        bool condition,
        Expression<Func<T, bool>> predicate) where T : class
    {
        return condition ? queryBuilder.Where(predicate) : queryBuilder;
    }

    /// <summary>
    /// Applies conditional ordering to the query.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <typeparam name="TKey">The key type.</typeparam>
    /// <param name="queryBuilder">The query builder.</param>
    /// <param name="condition">The condition to check.</param>
    /// <param name="keySelector">The key selector expression.</param>
    /// <param name="descending">Whether to order in descending order.</param>
    /// <returns>The query builder for method chaining.</returns>
    public static IQueryBuilder<T> OrderByIf<T, TKey>(
        this IQueryBuilder<T> queryBuilder,
        bool condition,
        Expression<Func<T, TKey>> keySelector,
        bool descending = false) where T : class
    {
        if (!condition) return queryBuilder;
        return descending ? queryBuilder.OrderByDescending(keySelector) : queryBuilder.OrderBy(keySelector);
    }
}