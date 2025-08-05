using Making.Ddd.Domain.Domain.Uow;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;

namespace Making.EntityFrameworkCore.EntityFrameworkCore;

/// <summary>
/// Entity Framework Core implementation of the Unit of Work pattern.
/// </summary>
public class EfCoreUnitOfWork : IUnitOfWork
{
    private readonly MakingDbContext _dbContext;
    private readonly ILogger<EfCoreUnitOfWork> _logger;
    private IDbContextTransaction? _currentTransaction;
    private bool _disposed = false;

    public EfCoreUnitOfWork(MakingDbContext dbContext, ILogger<EfCoreUnitOfWork> logger)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        CheckDisposed();
        
        try
        {
            var result = await _dbContext.SaveChangesAsync(cancellationToken);
            _logger.LogDebug("Saved {Count} changes to database", result);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save changes to database");
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        CheckDisposed();

        if (_currentTransaction != null)
        {
            _logger.LogWarning("Transaction already exists, ignoring new transaction request");
            return;
        }

        try
        {
            _currentTransaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
            _logger.LogDebug("Transaction began with ID: {TransactionId}", _currentTransaction.TransactionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to begin transaction");
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
    {
        CheckDisposed();

        if (_currentTransaction == null)
        {
            _logger.LogWarning("No active transaction to commit");
            return;
        }

        try
        {
            await _currentTransaction.CommitAsync(cancellationToken);
            _logger.LogDebug("Transaction committed with ID: {TransactionId}", _currentTransaction.TransactionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to commit transaction with ID: {TransactionId}", _currentTransaction.TransactionId);
            await RollbackTransactionAsync(cancellationToken);
            throw;
        }
        finally
        {
            await DisposeTransactionAsync();
        }
    }

    /// <inheritdoc/>
    public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
    {
        CheckDisposed();

        if (_currentTransaction == null)
        {
            _logger.LogWarning("No active transaction to rollback");
            return;
        }

        try
        {
            await _currentTransaction.RollbackAsync(cancellationToken);
            _logger.LogDebug("Transaction rolled back with ID: {TransactionId}", _currentTransaction.TransactionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to rollback transaction with ID: {TransactionId}", _currentTransaction.TransactionId);
            throw;
        }
        finally
        {
            await DisposeTransactionAsync();
        }
    }

    /// <summary>
    /// Gets the current database transaction.
    /// </summary>
    public IDbContextTransaction? CurrentTransaction => _currentTransaction;

    /// <summary>
    /// Gets a value indicating whether there is an active transaction.
    /// </summary>
    public bool HasActiveTransaction => _currentTransaction != null;

    /// <summary>
    /// Disposes the current transaction.
    /// </summary>
    private async Task DisposeTransactionAsync()
    {
        if (_currentTransaction != null)
        {
            await _currentTransaction.DisposeAsync();
            _currentTransaction = null;
        }
    }

    /// <summary>
    /// Checks if the unit of work is disposed.
    /// </summary>
    private void CheckDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(EfCoreUnitOfWork));
        }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Disposes the unit of work.
    /// </summary>
    /// <param name="disposing">True if disposing managed resources.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed && disposing)
        {
            if (_currentTransaction != null)
            {
                try
                {
                    RollbackTransactionAsync().ConfigureAwait(false).GetAwaiter().GetResult();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred while disposing transaction during UnitOfWork disposal");
                }
            }
            
            _disposed = true;
        }
    }

    /// <summary>
    /// Finalizer.
    /// </summary>
    ~EfCoreUnitOfWork()
    {
        Dispose(false);
    }
}