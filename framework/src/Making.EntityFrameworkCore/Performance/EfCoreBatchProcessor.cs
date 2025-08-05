using Making.Ddd.Domain.Domain.Entities;
using Making.EntityFrameworkCore.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace Making.EntityFrameworkCore.Performance;

/// <summary>
/// Entity Framework Core implementation of batch processor for high-performance operations.
/// </summary>
/// <typeparam name="TEntity">The entity type.</typeparam>
public class EfCoreBatchProcessor<TEntity> : IBatchProcessor<TEntity> where TEntity : class, IEntity
{
    private readonly MakingDbContext _dbContext;
    private readonly DbSet<TEntity> _dbSet;
    private readonly ILogger<EfCoreBatchProcessor<TEntity>> _logger;

    private readonly List<TEntity> _insertEntities = new();
    private readonly List<TEntity> _updateEntities = new();
    private readonly List<TEntity> _deleteEntities = new();
    private readonly List<TEntity> _upsertEntities = new();

    private int _batchSize = 1000;
    private bool _useTransaction = true;
    private TimeSpan _timeout = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Initializes a new instance of the EfCoreBatchProcessor class.
    /// </summary>
    /// <param name="dbContext">The database context.</param>
    /// <param name="logger">The logger instance.</param>
    public EfCoreBatchProcessor(MakingDbContext dbContext, ILogger<EfCoreBatchProcessor<TEntity>> logger)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _dbSet = dbContext.Set<TEntity>();
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public IBatchProcessor<TEntity> WithBatchSize(int batchSize)
    {
        if (batchSize <= 0)
            throw new ArgumentException("Batch size must be greater than zero", nameof(batchSize));

        _batchSize = batchSize;
        _logger.LogDebug("Batch size set to {BatchSize} for {EntityType}", batchSize, typeof(TEntity).Name);
        return this;
    }

    /// <inheritdoc/>
    public IBatchProcessor<TEntity> WithTransaction(bool useTransaction)
    {
        _useTransaction = useTransaction;
        _logger.LogDebug("Transaction usage set to {UseTransaction} for {EntityType}", useTransaction, typeof(TEntity).Name);
        return this;
    }

    /// <inheritdoc/>
    public IBatchProcessor<TEntity> WithTimeout(TimeSpan timeout)
    {
        _timeout = timeout;
        _logger.LogDebug("Timeout set to {Timeout} for {EntityType}", timeout, typeof(TEntity).Name);
        return this;
    }

    /// <inheritdoc/>
    public IBatchProcessor<TEntity> Insert(IEnumerable<TEntity> entities)
    {
        var entityList = entities.ToList();
        _insertEntities.AddRange(entityList);
        _logger.LogDebug("Added {Count} entities for batch insert for {EntityType}", entityList.Count, typeof(TEntity).Name);
        return this;
    }

    /// <inheritdoc/>
    public IBatchProcessor<TEntity> Update(IEnumerable<TEntity> entities)
    {
        var entityList = entities.ToList();
        _updateEntities.AddRange(entityList);
        _logger.LogDebug("Added {Count} entities for batch update for {EntityType}", entityList.Count, typeof(TEntity).Name);
        return this;
    }

    /// <inheritdoc/>
    public IBatchProcessor<TEntity> Delete(IEnumerable<TEntity> entities)
    {
        var entityList = entities.ToList();
        _deleteEntities.AddRange(entityList);
        _logger.LogDebug("Added {Count} entities for batch delete for {EntityType}", entityList.Count, typeof(TEntity).Name);
        return this;
    }

    /// <inheritdoc/>
    public IBatchProcessor<TEntity> Upsert(IEnumerable<TEntity> entities)
    {
        var entityList = entities.ToList();
        _upsertEntities.AddRange(entityList);
        _logger.LogDebug("Added {Count} entities for batch upsert for {EntityType}", entityList.Count, typeof(TEntity).Name);
        return this;
    }

    /// <inheritdoc/>
    public async Task<BatchResult> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        return await ExecuteAsync(null, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<BatchResult> ExecuteAsync(IProgress<BatchProgress>? progress, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var totalOperations = _insertEntities.Count + _updateEntities.Count + _deleteEntities.Count + _upsertEntities.Count;
        
        if (totalOperations == 0)
        {
            _logger.LogWarning("No operations to execute for batch processor for {EntityType}", typeof(TEntity).Name);
            return BatchResult.Success(0, stopwatch.Elapsed);
        }

        _logger.LogInformation("Starting batch execution for {EntityType} with {TotalOperations} operations", 
            typeof(TEntity).Name, totalOperations);

        try
        {
            var totalAffected = 0;
            var processedItems = 0;

            if (_useTransaction)
            {
                using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
                try
                {
                    totalAffected = await ExecuteOperationsAsync(progress, processedItems, totalOperations, cancellationToken);
                    await transaction.CommitAsync(cancellationToken);
                    _logger.LogDebug("Transaction committed for batch operations for {EntityType}", typeof(TEntity).Name);
                }
                catch (Exception)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    _logger.LogError("Transaction rolled back for batch operations for {EntityType}", typeof(TEntity).Name);
                    throw;
                }
            }
            else
            {
                totalAffected = await ExecuteOperationsAsync(progress, processedItems, totalOperations, cancellationToken);
            }

            stopwatch.Stop();
            _logger.LogInformation("Completed batch execution for {EntityType} in {ElapsedTime}, affected {AffectedRecords} records", 
                typeof(TEntity).Name, stopwatch.Elapsed, totalAffected);

            return BatchResult.Success(totalAffected, stopwatch.Elapsed);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Error executing batch operations for {EntityType} after {ElapsedTime}", 
                typeof(TEntity).Name, stopwatch.Elapsed);
            return BatchResult.Failure(ex.Message, stopwatch.Elapsed);
        }
    }

    /// <summary>
    /// Executes all configured operations.
    /// </summary>
    /// <param name="progress">The progress reporter.</param>
    /// <param name="processedItems">The current processed items count.</param>
    /// <param name="totalOperations">The total number of operations.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The total number of affected records.</returns>
    private async Task<int> ExecuteOperationsAsync(IProgress<BatchProgress>? progress, int processedItems, int totalOperations, CancellationToken cancellationToken)
    {
        var totalAffected = 0;

        // Execute inserts
        if (_insertEntities.Count > 0)
        {
            progress?.Report(new BatchProgress
            {
                CurrentStep = "Inserting entities",
                ProcessedItems = processedItems,
                TotalItems = totalOperations,
                OperationType = BatchOperationType.Insert
            });

            totalAffected += await ExecuteBatchInsertAsync(_insertEntities, cancellationToken);
            processedItems += _insertEntities.Count;
        }

        // Execute updates
        if (_updateEntities.Count > 0)
        {
            progress?.Report(new BatchProgress
            {
                CurrentStep = "Updating entities",
                ProcessedItems = processedItems,
                TotalItems = totalOperations,
                OperationType = BatchOperationType.Update
            });

            totalAffected += await ExecuteBatchUpdateAsync(_updateEntities, cancellationToken);
            processedItems += _updateEntities.Count;
        }

        // Execute upserts
        if (_upsertEntities.Count > 0)
        {
            progress?.Report(new BatchProgress
            {
                CurrentStep = "Upserting entities",
                ProcessedItems = processedItems,
                TotalItems = totalOperations,
                OperationType = BatchOperationType.Upsert
            });

            totalAffected += await ExecuteBatchUpsertAsync(_upsertEntities, cancellationToken);
            processedItems += _upsertEntities.Count;
        }

        // Execute deletes
        if (_deleteEntities.Count > 0)
        {
            progress?.Report(new BatchProgress
            {
                CurrentStep = "Deleting entities",
                ProcessedItems = processedItems,
                TotalItems = totalOperations,
                OperationType = BatchOperationType.Delete
            });

            totalAffected += await ExecuteBatchDeleteAsync(_deleteEntities, cancellationToken);
            processedItems += _deleteEntities.Count;
        }

        // Final save changes
        var savedChanges = await _dbContext.SaveChangesAsync(cancellationToken);
        _logger.LogDebug("SaveChanges completed with {SavedChanges} changes for {EntityType}", savedChanges, typeof(TEntity).Name);

        progress?.Report(new BatchProgress
        {
            CurrentStep = "Completed",
            ProcessedItems = totalOperations,
            TotalItems = totalOperations,
            OperationType = BatchOperationType.Insert // Default
        });

        return Math.Max(totalAffected, savedChanges);
    }

    /// <summary>
    /// Executes batch insert operations.
    /// </summary>
    /// <param name="entities">The entities to insert.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The number of affected records.</returns>
    private async Task<int> ExecuteBatchInsertAsync(List<TEntity> entities, CancellationToken cancellationToken)
    {
        var totalAffected = 0;
        
        for (int i = 0; i < entities.Count; i += _batchSize)
        {
            var batch = entities.Skip(i).Take(_batchSize).ToList();
            await _dbSet.AddRangeAsync(batch, cancellationToken);
            totalAffected += batch.Count;
            
            _logger.LogDebug("Added batch of {BatchSize} entities for insert for {EntityType} (batch {BatchNumber})", 
                batch.Count, typeof(TEntity).Name, (i / _batchSize) + 1);
        }

        return totalAffected;
    }

    /// <summary>
    /// Executes batch update operations.
    /// </summary>
    /// <param name="entities">The entities to update.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The number of affected records.</returns>
    private async Task<int> ExecuteBatchUpdateAsync(List<TEntity> entities, CancellationToken cancellationToken)
    {
        var totalAffected = 0;
        
        for (int i = 0; i < entities.Count; i += _batchSize)
        {
            var batch = entities.Skip(i).Take(_batchSize).ToList();
            _dbSet.UpdateRange(batch);
            totalAffected += batch.Count;
            
            _logger.LogDebug("Added batch of {BatchSize} entities for update for {EntityType} (batch {BatchNumber})", 
                batch.Count, typeof(TEntity).Name, (i / _batchSize) + 1);
        }

        return totalAffected;
    }

    /// <summary>
    /// Executes batch delete operations.
    /// </summary>
    /// <param name="entities">The entities to delete.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The number of affected records.</returns>
    private async Task<int> ExecuteBatchDeleteAsync(List<TEntity> entities, CancellationToken cancellationToken)
    {
        var totalAffected = 0;
        
        for (int i = 0; i < entities.Count; i += _batchSize)
        {
            var batch = entities.Skip(i).Take(_batchSize).ToList();
            _dbSet.RemoveRange(batch);
            totalAffected += batch.Count;
            
            _logger.LogDebug("Added batch of {BatchSize} entities for delete for {EntityType} (batch {BatchNumber})", 
                batch.Count, typeof(TEntity).Name, (i / _batchSize) + 1);
        }

        return totalAffected;
    }

    /// <summary>
    /// Executes batch upsert operations.
    /// </summary>
    /// <param name="entities">The entities to upsert.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The number of affected records.</returns>
    private async Task<int> ExecuteBatchUpsertAsync(List<TEntity> entities, CancellationToken cancellationToken)
    {
        var totalAffected = 0;
        
        for (int i = 0; i < entities.Count; i += _batchSize)
        {
            var batch = entities.Skip(i).Take(_batchSize).ToList();
            
            // Simple upsert implementation - check if entity exists and update or insert accordingly
            foreach (var entity in batch)
            {
                var existingEntity = await _dbSet.FindAsync(new object[] { entity.GetKeys().First() }, cancellationToken);
                if (existingEntity != null)
                {
                    _dbContext.Entry(existingEntity).CurrentValues.SetValues(entity);
                }
                else
                {
                    await _dbSet.AddAsync(entity, cancellationToken);
                }
                totalAffected++;
            }
            
            _logger.LogDebug("Processed batch of {BatchSize} entities for upsert for {EntityType} (batch {BatchNumber})", 
                batch.Count, typeof(TEntity).Name, (i / _batchSize) + 1);
        }

        return totalAffected;
    }
}