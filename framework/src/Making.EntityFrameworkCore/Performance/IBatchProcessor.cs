using Making.Ddd.Domain.Domain.Entities;
using Making.Ddd.Domain.Domain.Repositories;

namespace Making.EntityFrameworkCore.Performance;

/// <summary>
/// Represents the result of a batch operation.
/// </summary>
public class BatchResult
{
    /// <summary>
    /// Gets or sets the number of affected records.
    /// </summary>
    public int AffectedRecords { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the operation was successful.
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// Gets or sets the error message if the operation failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets the execution time of the batch operation.
    /// </summary>
    public TimeSpan ExecutionTime { get; set; }

    /// <summary>
    /// Gets or sets additional metadata about the operation.
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();

    /// <summary>
    /// Creates a successful batch result.
    /// </summary>
    /// <param name="affectedRecords">The number of affected records.</param>
    /// <param name="executionTime">The execution time.</param>
    /// <returns>A successful batch result.</returns>
    public static BatchResult Success(int affectedRecords, TimeSpan executionTime)
    {
        return new BatchResult
        {
            AffectedRecords = affectedRecords,
            IsSuccess = true,
            ExecutionTime = executionTime
        };
    }

    /// <summary>
    /// Creates a failed batch result.
    /// </summary>
    /// <param name="errorMessage">The error message.</param>
    /// <param name="executionTime">The execution time.</param>
    /// <returns>A failed batch result.</returns>
    public static BatchResult Failure(string errorMessage, TimeSpan executionTime)
    {
        return new BatchResult
        {
            AffectedRecords = 0,
            IsSuccess = false,
            ErrorMessage = errorMessage,
            ExecutionTime = executionTime
        };
    }
}

/// <summary>
/// Interface for high-performance batch operations.
/// </summary>
/// <typeparam name="TEntity">The entity type.</typeparam>
public interface IBatchProcessor<TEntity> where TEntity : class, IEntity
{
    /// <summary>
    /// Configures the batch size for operations.
    /// </summary>
    /// <param name="batchSize">The batch size (default: 1000).</param>
    /// <returns>The batch processor for method chaining.</returns>
    IBatchProcessor<TEntity> WithBatchSize(int batchSize);

    /// <summary>
    /// Configures whether to use transactions for batch operations.
    /// </summary>
    /// <param name="useTransaction">Whether to use transactions (default: true).</param>
    /// <returns>The batch processor for method chaining.</returns>
    IBatchProcessor<TEntity> WithTransaction(bool useTransaction);

    /// <summary>
    /// Configures the timeout for batch operations.
    /// </summary>
    /// <param name="timeout">The timeout duration.</param>
    /// <returns>The batch processor for method chaining.</returns>
    IBatchProcessor<TEntity> WithTimeout(TimeSpan timeout);

    /// <summary>
    /// Adds entities for batch insertion.
    /// </summary>
    /// <param name="entities">The entities to insert.</param>
    /// <returns>The batch processor for method chaining.</returns>
    IBatchProcessor<TEntity> Insert(IEnumerable<TEntity> entities);

    /// <summary>
    /// Adds entities for batch update.
    /// </summary>
    /// <param name="entities">The entities to update.</param>
    /// <returns>The batch processor for method chaining.</returns>
    IBatchProcessor<TEntity> Update(IEnumerable<TEntity> entities);

    /// <summary>
    /// Adds entities for batch deletion.
    /// </summary>
    /// <param name="entities">The entities to delete.</param>
    /// <returns>The batch processor for method chaining.</returns>
    IBatchProcessor<TEntity> Delete(IEnumerable<TEntity> entities);

    /// <summary>
    /// Adds entities for batch upsert (insert or update).
    /// </summary>
    /// <param name="entities">The entities to upsert.</param>
    /// <returns>The batch processor for method chaining.</returns>
    IBatchProcessor<TEntity> Upsert(IEnumerable<TEntity> entities);

    /// <summary>
    /// Executes all configured batch operations.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The batch operation result.</returns>
    Task<BatchResult> ExecuteAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes all configured batch operations with progress reporting.
    /// </summary>
    /// <param name="progress">The progress reporter.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The batch operation result.</returns>
    Task<BatchResult> ExecuteAsync(IProgress<BatchProgress> progress, CancellationToken cancellationToken = default);
}

/// <summary>
/// Represents progress information for batch operations.
/// </summary>
public class BatchProgress
{
    /// <summary>
    /// Gets or sets the current step description.
    /// </summary>
    public string CurrentStep { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the number of processed items.
    /// </summary>
    public int ProcessedItems { get; set; }

    /// <summary>
    /// Gets or sets the total number of items to process.
    /// </summary>
    public int TotalItems { get; set; }

    /// <summary>
    /// Gets the completion percentage (0-100).
    /// </summary>
    public double PercentageComplete => TotalItems > 0 ? (double)ProcessedItems / TotalItems * 100 : 0;

    /// <summary>
    /// Gets or sets the estimated time remaining.
    /// </summary>
    public TimeSpan? EstimatedTimeRemaining { get; set; }

    /// <summary>
    /// Gets or sets the current operation type.
    /// </summary>
    public BatchOperationType OperationType { get; set; }
}

/// <summary>
/// Enumeration of batch operation types.
/// </summary>
public enum BatchOperationType
{
    Insert,
    Update,
    Delete,
    Upsert
}

/// <summary>
/// Extensions for creating batch processors.
/// </summary>
public static class BatchProcessorExtensions
{
    /// <summary>
    /// Creates a batch processor for the specified entity type.
    /// </summary>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    /// <param name="repository">The repository instance.</param>
    /// <returns>A new batch processor instance.</returns>
    public static IBatchProcessor<TEntity> CreateBatchProcessor<TEntity>(this IRepository<TEntity> repository) 
        where TEntity : class, IEntity
    {
        if (repository is IBatchProcessorFactory<TEntity> factory)
        {
            return factory.CreateBatchProcessor();
        }

        throw new NotSupportedException($"Repository of type {repository.GetType().Name} does not support batch processing");
    }
}

/// <summary>
/// Factory interface for creating batch processors.
/// </summary>
/// <typeparam name="TEntity">The entity type.</typeparam>
public interface IBatchProcessorFactory<TEntity> where TEntity : class, IEntity
{
    /// <summary>
    /// Creates a new batch processor instance.
    /// </summary>
    /// <returns>A new batch processor instance.</returns>
    IBatchProcessor<TEntity> CreateBatchProcessor();
}