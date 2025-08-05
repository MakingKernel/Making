using System.Linq.Expressions;

namespace Making.EntityFrameworkCore.Query;

/// <summary>
/// Represents a specification pattern for building complex queries with reusable business rules.
/// </summary>
/// <typeparam name="T">The entity type.</typeparam>
public interface ISpecification<T>
{
    /// <summary>
    /// Gets the criteria expression for filtering entities.
    /// </summary>
    Expression<Func<T, bool>>? Criteria { get; }

    /// <summary>
    /// Gets the list of navigation properties to include in the query.
    /// </summary>
    List<Expression<Func<T, object>>> Includes { get; }

    /// <summary>
    /// Gets the list of navigation properties to include as strings for complex includes.
    /// </summary>
    List<string> IncludeStrings { get; }

    /// <summary>
    /// Gets the ordering function for sorting results.
    /// </summary>
    Func<IQueryable<T>, IOrderedQueryable<T>>? OrderBy { get; }

    /// <summary>
    /// Gets the group by expression.
    /// </summary>
    Expression<Func<T, object>>? GroupBy { get; }

    /// <summary>
    /// Gets the number of records to skip for pagination.
    /// </summary>
    int? Skip { get; }

    /// <summary>
    /// Gets the number of records to take for pagination.
    /// </summary>
    int? Take { get; }

    /// <summary>
    /// Gets a value indicating whether to disable change tracking for read-only queries.
    /// </summary>
    bool AsNoTracking { get; }

    /// <summary>
    /// Gets a value indicating whether to split queries for complex includes.
    /// </summary>
    bool AsSplitQuery { get; }

    /// <summary>
    /// Gets the cache key for query result caching.
    /// </summary>
    string? CacheKey { get; }

    /// <summary>
    /// Gets the cache expiration time.
    /// </summary>
    TimeSpan? CacheExpiration { get; }

    /// <summary>
    /// Determines whether the specification is satisfied by the given entity.
    /// </summary>
    /// <param name="entity">The entity to check.</param>
    /// <returns>True if the entity satisfies the specification; otherwise, false.</returns>
    bool IsSatisfiedBy(T entity);
}

/// <summary>
/// Base class for implementing specifications with fluent API.
/// </summary>
/// <typeparam name="T">The entity type.</typeparam>
public abstract class Specification<T> : ISpecification<T>
{
    public Expression<Func<T, bool>>? Criteria { get; private set; }
    public List<Expression<Func<T, object>>> Includes { get; } = new();
    public List<string> IncludeStrings { get; } = new();
    public Func<IQueryable<T>, IOrderedQueryable<T>>? OrderBy { get; private set; }
    public Expression<Func<T, object>>? GroupBy { get; private set; }
    public int? Skip { get; private set; }
    public int? Take { get; private set; }
    public bool AsNoTracking { get; private set; }
    public bool AsSplitQuery { get; private set; }
    public string? CacheKey { get; private set; }
    public TimeSpan? CacheExpiration { get; private set; }

    /// <summary>
    /// Initializes a new instance of the Specification class.
    /// </summary>
    protected Specification()
    {
    }

    /// <summary>
    /// Initializes a new instance of the Specification class with criteria.
    /// </summary>
    /// <param name="criteria">The filter criteria.</param>
    protected Specification(Expression<Func<T, bool>> criteria)
    {
        Criteria = criteria;
    }

    /// <summary>
    /// Sets the filter criteria.
    /// </summary>
    /// <param name="criteria">The filter criteria.</param>
    /// <returns>The current specification for method chaining.</returns>
    protected Specification<T> Where(Expression<Func<T, bool>> criteria)
    {
        Criteria = Criteria == null ? criteria : CombineExpressions(Criteria, criteria, Expression.AndAlso);
        return this;
    }

    /// <summary>
    /// Adds a navigation property to include in the query.
    /// </summary>
    /// <param name="includeExpression">The include expression.</param>
    /// <returns>The current specification for method chaining.</returns>
    protected Specification<T> Include(Expression<Func<T, object>> includeExpression)
    {
        Includes.Add(includeExpression);
        return this;
    }

    /// <summary>
    /// Adds a navigation property to include using string notation.
    /// </summary>
    /// <param name="include">The include string.</param>
    /// <returns>The current specification for method chaining.</returns>
    protected Specification<T> Include(string include)
    {
        IncludeStrings.Add(include);
        return this;
    }

    /// <summary>
    /// Sets the ordering for the query.
    /// </summary>
    /// <param name="orderByExpression">The order by expression.</param>
    /// <returns>The current specification for method chaining.</returns>
    protected Specification<T> OrderByAsc<TKey>(Expression<Func<T, TKey>> orderByExpression)
    {
        OrderBy = query => query.OrderBy(orderByExpression);
        return this;
    }

    /// <summary>
    /// Sets the descending ordering for the query.
    /// </summary>
    /// <param name="orderByExpression">The order by expression.</param>
    /// <returns>The current specification for method chaining.</returns>
    protected Specification<T> OrderByDesc<TKey>(Expression<Func<T, TKey>> orderByExpression)
    {
        OrderBy = query => query.OrderByDescending(orderByExpression);
        return this;
    }

    /// <summary>
    /// Sets the group by expression.
    /// </summary>
    /// <param name="groupByExpression">The group by expression.</param>
    /// <returns>The current specification for method chaining.</returns>
    protected Specification<T> GroupByClause(Expression<Func<T, object>> groupByExpression)
    {
        GroupBy = groupByExpression;
        return this;
    }

    /// <summary>
    /// Sets pagination for the query.
    /// </summary>
    /// <param name="skip">Number of records to skip.</param>
    /// <param name="take">Number of records to take.</param>
    /// <returns>The current specification for method chaining.</returns>
    protected Specification<T> Paginate(int skip, int take)
    {
        Skip = skip;
        Take = take;
        return this;
    }

    /// <summary>
    /// Configures the query to use no tracking for read-only operations.
    /// </summary>
    /// <returns>The current specification for method chaining.</returns>
    protected Specification<T> AsReadOnly()
    {
        AsNoTracking = true;
        return this;
    }

    /// <summary>
    /// Configures the query to use split query for complex includes.
    /// </summary>
    /// <returns>The current specification for method chaining.</returns>
    protected Specification<T> UseSplitQuery()
    {
        AsSplitQuery = true;
        return this;
    }

    /// <summary>
    /// Sets caching options for the query result.
    /// </summary>
    /// <param name="cacheKey">The cache key.</param>
    /// <param name="expiration">The cache expiration time.</param>
    /// <returns>The current specification for method chaining.</returns>
    protected Specification<T> WithCache(string cacheKey, TimeSpan? expiration = null)
    {
        CacheKey = cacheKey;
        CacheExpiration = expiration ?? TimeSpan.FromMinutes(30);
        return this;
    }

    /// <summary>
    /// Determines whether the specification is satisfied by the given entity.
    /// </summary>
    /// <param name="entity">The entity to check.</param>
    /// <returns>True if the entity satisfies the specification; otherwise, false.</returns>
    public virtual bool IsSatisfiedBy(T entity)
    {
        return Criteria?.Compile().Invoke(entity) ?? true;
    }

    /// <summary>
    /// Combines two expressions using the specified operation.
    /// </summary>
    /// <param name="left">The left expression.</param>
    /// <param name="right">The right expression.</param>
    /// <param name="operation">The operation to combine expressions.</param>
    /// <returns>The combined expression.</returns>
    private static Expression<Func<T, bool>> CombineExpressions(
        Expression<Func<T, bool>> left,
        Expression<Func<T, bool>> right,
        Func<Expression, Expression, Expression> operation)
    {
        var parameter = Expression.Parameter(typeof(T));
        var leftVisitor = new ReplaceExpressionVisitor(left.Parameters[0], parameter);
        var rightVisitor = new ReplaceExpressionVisitor(right.Parameters[0], parameter);

        var leftBody = leftVisitor.Visit(left.Body);
        var rightBody = rightVisitor.Visit(right.Body);

        return Expression.Lambda<Func<T, bool>>(
            operation(leftBody, rightBody), parameter);
    }

    /// <summary>
    /// Expression visitor for replacing parameters in lambda expressions.
    /// </summary>
    private class ReplaceExpressionVisitor : ExpressionVisitor
    {
        private readonly Expression _oldValue;
        private readonly Expression _newValue;

        public ReplaceExpressionVisitor(Expression oldValue, Expression newValue)
        {
            _oldValue = oldValue;
            _newValue = newValue;
        }

        public override Expression? Visit(Expression? node)
        {
            return node == _oldValue ? _newValue : base.Visit(node);
        }
    }
}

/// <summary>
/// Specification extensions for combining specifications.
/// </summary>
public static class SpecificationExtensions
{
    /// <summary>
    /// Combines two specifications using AND logic.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="left">The left specification.</param>
    /// <param name="right">The right specification.</param>
    /// <returns>A new combined specification.</returns>
    public static ISpecification<T> And<T>(this ISpecification<T> left, ISpecification<T> right)
    {
        return new AndSpecification<T>(left, right);
    }

    /// <summary>
    /// Combines two specifications using OR logic.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="left">The left specification.</param>
    /// <param name="right">The right specification.</param>
    /// <returns>A new combined specification.</returns>
    public static ISpecification<T> Or<T>(this ISpecification<T> left, ISpecification<T> right)
    {
        return new OrSpecification<T>(left, right);
    }

    /// <summary>
    /// Negates a specification using NOT logic.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="specification">The specification to negate.</param>
    /// <returns>A new negated specification.</returns>
    public static ISpecification<T> Not<T>(this ISpecification<T> specification)
    {
        return new NotSpecification<T>(specification);
    }
}

/// <summary>
/// Specification that combines two specifications using AND logic.
/// </summary>
/// <typeparam name="T">The entity type.</typeparam>
internal class AndSpecification<T> : Specification<T>
{
    private readonly ISpecification<T> _left;
    private readonly ISpecification<T> _right;

    public AndSpecification(ISpecification<T> left, ISpecification<T> right)
    {
        _left = left;
        _right = right;
    }

    public override bool IsSatisfiedBy(T entity)
    {
        return _left.IsSatisfiedBy(entity) && _right.IsSatisfiedBy(entity);
    }
}

/// <summary>
/// Specification that combines two specifications using OR logic.
/// </summary>
/// <typeparam name="T">The entity type.</typeparam>
internal class OrSpecification<T> : Specification<T>
{
    private readonly ISpecification<T> _left;
    private readonly ISpecification<T> _right;

    public OrSpecification(ISpecification<T> left, ISpecification<T> right)
    {
        _left = left;
        _right = right;
    }

    public override bool IsSatisfiedBy(T entity)
    {
        return _left.IsSatisfiedBy(entity) || _right.IsSatisfiedBy(entity);
    }
}

/// <summary>
/// Specification that negates another specification using NOT logic.
/// </summary>
/// <typeparam name="T">The entity type.</typeparam>
internal class NotSpecification<T> : Specification<T>
{
    private readonly ISpecification<T> _specification;

    public NotSpecification(ISpecification<T> specification)
    {
        _specification = specification;
    }

    public override bool IsSatisfiedBy(T entity)
    {
        return !_specification.IsSatisfiedBy(entity);
    }
}