using Making.Ddd.Domain.Domain.Uow;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using System.Data;

namespace Making.EntityFrameworkCore.EntityFrameworkCore.Improvements;

/// <summary>
/// Enhanced Unit of Work implementation with nested transaction support and comprehensive error handling
/// This addresses the limitations of the basic EfCoreUnitOfWork
/// </summary>
public class EnhancedEfCoreUnitOfWork : IUnitOfWork, IDisposable, IAsyncDisposable
{
    private readonly MakingDbContext _dbContext;
    private readonly ILogger<EnhancedEfCoreUnitOfWork> _logger;
    private IDbContextTransaction? _transaction;
    private readonly Stack<IDbContextTransaction> _transactionStack = new();
    private readonly Stack<string> _savepointStack = new();
    private bool _disposed;
    private int _savepointCounter;

    public EnhancedEfCoreUnitOfWork(MakingDbContext dbContext, ILogger<EnhancedEfCoreUnitOfWork> logger)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Begin a new unit of work transaction
    /// If a transaction is already active, creates a savepoint for nested transaction support
    /// </summary>
    public async Task BeginAsync(IsolationLevel isolationLevel = IsolationLevel.ReadCommitted, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        try
        {
            if (_transaction == null)
            {
                // Start root transaction
                _transaction = await _dbContext.Database.BeginTransactionAsync(isolationLevel, cancellationToken);
                _transactionStack.Push(_transaction);
                _logger.LogDebug("Started root transaction with isolation level {IsolationLevel}", isolationLevel);
            }
            else
            {
                // Create savepoint for nested transaction
                var savepointName = $"sp_{++_savepointCounter}_{DateTime.UtcNow.Ticks}";
                await _transaction.CreateSavepointAsync(savepointName, cancellationToken);
                _savepointStack.Push(savepointName);
                _logger.LogDebug("Created savepoint {SavepointName} for nested transaction", savepointName);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to begin transaction");
            throw;
        }
    }

    /// <summary>
    /// Begin a new unit of work transaction synchronously
    /// </summary>
    public void Begin(IsolationLevel isolationLevel = IsolationLevel.ReadCommitted)
    {
        ThrowIfDisposed();

        try
        {
            if (_transaction == null)
            {
                // Start root transaction
                _transaction = _dbContext.Database.BeginTransaction(isolationLevel);
                _transactionStack.Push(_transaction);
                _logger.LogDebug("Started root transaction with isolation level {IsolationLevel}", isolationLevel);
            }
            else
            {
                // Create savepoint for nested transaction
                var savepointName = $"sp_{++_savepointCounter}_{DateTime.UtcNow.Ticks}";
                _transaction.CreateSavepoint(savepointName);
                _savepointStack.Push(savepointName);
                _logger.LogDebug("Created savepoint {SavepointName} for nested transaction", savepointName);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to begin transaction");
            throw;
        }
    }

    /// <summary>
    /// Commit the current unit of work
    /// If nested transactions are active, releases the most recent savepoint
    /// </summary>
    public async Task CommitAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        if (_transaction == null)
        {
            _logger.LogWarning("Attempted to commit when no transaction is active");
            return;
        }

        try
        {
            if (_savepointStack.Count > 0)
            {
                // Release savepoint for nested transaction
                var savepointName = _savepointStack.Pop();
                await _transaction.ReleaseSavepointAsync(savepointName, cancellationToken);
                _logger.LogDebug("Released savepoint {SavepointName}", savepointName);
            }
            else
            {
                // Commit root transaction
                await _transaction.CommitAsync(cancellationToken);
                _logger.LogDebug("Committed root transaction");
                await CleanupTransactionAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to commit transaction");
            await RollbackAsync(cancellationToken);
            throw;
        }
    }

    /// <summary>
    /// Commit the current unit of work synchronously
    /// </summary>
    public void Commit()
    {
        ThrowIfDisposed();

        if (_transaction == null)
        {
            _logger.LogWarning("Attempted to commit when no transaction is active");
            return;
        }

        try
        {
            if (_savepointStack.Count > 0)
            {
                // Release savepoint for nested transaction
                var savepointName = _savepointStack.Pop();
                _transaction.ReleaseSavepoint(savepointName);
                _logger.LogDebug("Released savepoint {SavepointName}", savepointName);
            }
            else
            {
                // Commit root transaction
                _transaction.Commit();
                _logger.LogDebug("Committed root transaction");
                CleanupTransaction();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to commit transaction");
            Rollback();
            throw;
        }
    }

    /// <summary>
    /// Rollback the current unit of work
    /// If nested transactions are active, rolls back to the most recent savepoint
    /// </summary>
    public async Task RollbackAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        if (_transaction == null)
        {
            _logger.LogWarning("Attempted to rollback when no transaction is active");
            return;
        }

        try
        {
            if (_savepointStack.Count > 0)
            {
                // Rollback to savepoint for nested transaction
                var savepointName = _savepointStack.Pop();
                await _transaction.RollbackToSavepointAsync(savepointName, cancellationToken);
                _logger.LogDebug("Rolled back to savepoint {SavepointName}", savepointName);
            }
            else
            {
                // Rollback root transaction
                await _transaction.RollbackAsync(cancellationToken);
                _logger.LogDebug("Rolled back root transaction");
                await CleanupTransactionAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to rollback transaction");
            await CleanupTransactionAsync();
            throw;
        }
    }

    /// <summary>
    /// Rollback the current unit of work synchronously
    /// </summary>
    public void Rollback()
    {
        ThrowIfDisposed();

        if (_transaction == null)
        {
            _logger.LogWarning("Attempted to rollback when no transaction is active");
            return;
        }

        try
        {
            if (_savepointStack.Count > 0)
            {
                // Rollback to savepoint for nested transaction
                var savepointName = _savepointStack.Pop();
                _transaction.RollbackToSavepoint(savepointName);
                _logger.LogDebug("Rolled back to savepoint {SavepointName}", savepointName);
            }
            else
            {
                // Rollback root transaction
                _transaction.Rollback();
                _logger.LogDebug("Rolled back root transaction");
                CleanupTransaction();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to rollback transaction");
            CleanupTransaction();
            throw;
        }
    }

    /// <summary>
    /// Save changes to the database within the current transaction
    /// </summary>
    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        try
        {
            var result = await _dbContext.SaveChangesAsync(cancellationToken);
            _logger.LogDebug("Saved {Count} changes to database", result);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save changes");
            throw;
        }
    }

    /// <summary>
    /// Save changes to the database within the current transaction synchronously
    /// </summary>
    public int SaveChanges()
    {
        ThrowIfDisposed();

        try
        {
            var result = _dbContext.SaveChanges();
            _logger.LogDebug("Saved {Count} changes to database", result);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save changes");
            throw;
        }
    }

    /// <summary>
    /// Execute work within a transaction scope with automatic commit/rollback
    /// </summary>
    public async Task<T> ExecuteAsync<T>(Func<Task<T>> work, IsolationLevel isolationLevel = IsolationLevel.ReadCommitted, CancellationToken cancellationToken = default)
    {
        await BeginAsync(isolationLevel, cancellationToken);

        try
        {
            var result = await work();
            await CommitAsync(cancellationToken);
            return result;
        }
        catch
        {
            await RollbackAsync(cancellationToken);
            throw;
        }
    }

    /// <summary>
    /// Execute work within a transaction scope with automatic commit/rollback synchronously
    /// </summary>
    public T Execute<T>(Func<T> work, IsolationLevel isolationLevel = IsolationLevel.ReadCommitted)
    {
        Begin(isolationLevel);

        try
        {
            var result = work();
            Commit();
            return result;
        }
        catch
        {
            Rollback();
            throw;
        }
    }

    /// <summary>
    /// Check if a transaction is currently active
    /// </summary>
    public bool IsTransactionActive => _transaction != null && _transactionStack.Count > 0;

    /// <summary>
    /// Get the current transaction nesting level
    /// </summary>
    public int TransactionNestingLevel => _savepointStack.Count + (_transaction != null ? 1 : 0);

    private async Task CleanupTransactionAsync()
    {
        if (_transaction != null)
        {
            await _transaction.DisposeAsync();
            _transaction = null;
        }

        _transactionStack.Clear();
        _savepointStack.Clear();
        _savepointCounter = 0;
    }

    private void CleanupTransaction()
    {
        if (_transaction != null)
        {
            _transaction.Dispose();
            _transaction = null;
        }

        _transactionStack.Clear();
        _savepointStack.Clear();
        _savepointCounter = 0;
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(EnhancedEfCoreUnitOfWork));
        }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            try
            {
                if (_transaction != null)
                {
                    _logger.LogWarning("Disposing UnitOfWork with active transaction - rolling back");
                    Rollback();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during UnitOfWork disposal");
            }
            finally
            {
                CleanupTransaction();
                _disposed = true;
            }
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (!_disposed)
        {
            try
            {
                if (_transaction != null)
                {
                    _logger.LogWarning("Disposing UnitOfWork with active transaction - rolling back");
                    await RollbackAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during UnitOfWork async disposal");
            }
            finally
            {
                await CleanupTransactionAsync();
                _disposed = true;
            }
        }
    }
}