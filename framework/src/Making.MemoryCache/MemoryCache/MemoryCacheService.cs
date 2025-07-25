using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Making.MemoryCache.MemoryCache;

[Singleton]
public class MemoryCacheService : IMemoryCacheService, IDisposable
{
    private readonly IMemoryCache _memoryCache;
    private readonly ILogger<MemoryCacheService> _logger;
    private readonly ConcurrentDictionary<string, object> _keyTracker;
    private long _hitCount;
    private long _missCount;
    private long _itemCount;
    private readonly Timer _cleanupTimer;
    private bool _disposed;

    public MemoryCacheService(IMemoryCache memoryCache, ILogger<MemoryCacheService> logger)
    {
        _memoryCache = memoryCache ?? throw new ArgumentNullException(nameof(memoryCache));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _keyTracker = new ConcurrentDictionary<string, object>();
        
        // 每5分钟清理一次过期的键跟踪
        _cleanupTimer = new Timer(CleanupExpiredKeys, null, TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5));
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null)
    {
        ArgumentNullException.ThrowIfNull(key);
        
        try
        {
            var options = new MemoryCacheEntryOptions();
            
            if (expiration.HasValue)
            {
                options.AbsoluteExpirationRelativeToNow = expiration.Value;
            }
            
            // 添加移除回调来更新键跟踪器
            options.RegisterPostEvictionCallback((k, v, reason, state) =>
            {
                var key = k?.ToString();
                if (!string.IsNullOrEmpty(key))
                {
                    _keyTracker.TryRemove(key, out object? _);
                    if (reason == EvictionReason.Expired)
                    {
                        _logger.LogDebug("Cache key {Key} expired", key);
                    }
                }
            });

            _memoryCache.Set(key, value, options);
            _keyTracker.TryAdd(key, null!);
            
            Interlocked.Increment(ref _itemCount);
            
            _logger.LogDebug("Set cache key: {Key}, expiration: {Expiration}", key, expiration);
            
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting cache key: {Key}", key);
            throw;
        }
    }

    public async Task<T?> GetAsync<T>(string key)
    {
        ArgumentNullException.ThrowIfNull(key);
        
        try
        {
            if (_memoryCache.TryGetValue(key, out var value))
            {
                Interlocked.Increment(ref _hitCount);
                _logger.LogDebug("Cache hit for key: {Key}", key);
                return (T?)value;
            }
            
            Interlocked.Increment(ref _missCount);
            _logger.LogDebug("Cache miss for key: {Key}", key);
            
            return await Task.FromResult(default(T));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting cache key: {Key}", key);
            Interlocked.Increment(ref _missCount);
            return default(T);
        }
    }

    public async Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiration = null)
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(factory);
        
        var cachedValue = await GetAsync<T>(key);
        if (cachedValue != null)
        {
            return cachedValue;
        }
        
        try
        {
            var value = await factory();
            await SetAsync(key, value, expiration);
            return value;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetOrSetAsync for key: {Key}", key);
            throw;
        }
    }

    public async Task RemoveAsync(string key)
    {
        ArgumentNullException.ThrowIfNull(key);
        
        try
        {
            _memoryCache.Remove(key);
            _keyTracker.TryRemove(key, out object? _);
            Interlocked.Decrement(ref _itemCount);
            
            _logger.LogDebug("Removed cache key: {Key}", key);
            
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing cache key: {Key}", key);
            throw;
        }
    }

    public async Task RemoveByPatternAsync(string pattern)
    {
        ArgumentNullException.ThrowIfNull(pattern);
        
        try
        {
            var regex = new Regex(pattern.Replace("*", ".*"), RegexOptions.IgnoreCase | RegexOptions.Compiled);
            var keysToRemove = _keyTracker.Keys.Where(key => regex.IsMatch(key)).ToList();
            
            foreach (var key in keysToRemove)
            {
                await RemoveAsync(key);
            }
            
            _logger.LogDebug("Removed {Count} cache keys matching pattern: {Pattern}", keysToRemove.Count, pattern);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing cache keys by pattern: {Pattern}", pattern);
            throw;
        }
    }

    public async Task<bool> ExistsAsync(string key)
    {
        ArgumentNullException.ThrowIfNull(key);
        
        try
        {
            var exists = _memoryCache.TryGetValue(key, out _);
            return await Task.FromResult(exists);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if cache key exists: {Key}", key);
            return false;
        }
    }

    public async Task RefreshAsync(string key, TimeSpan expiration)
    {
        ArgumentNullException.ThrowIfNull(key);
        
        try
        {
            if (_memoryCache.TryGetValue(key, out var value))
            {
                await SetAsync(key, value, expiration);
                _logger.LogDebug("Refreshed cache key: {Key} with new expiration: {Expiration}", key, expiration);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing cache key: {Key}", key);
            throw;
        }
    }

    public async Task<IEnumerable<string>> GetKeysAsync(string? pattern = null)
    {
        try
        {
            var keys = _keyTracker.Keys.AsEnumerable();
            
            if (!string.IsNullOrEmpty(pattern))
            {
                var regex = new Regex(pattern.Replace("*", ".*"), RegexOptions.IgnoreCase | RegexOptions.Compiled);
                keys = keys.Where(key => regex.IsMatch(key));
            }
            
            return await Task.FromResult(keys.ToList());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting cache keys with pattern: {Pattern}", pattern);
            return Enumerable.Empty<string>();
        }
    }

    public async Task ClearAsync()
    {
        try
        {
            var keys = _keyTracker.Keys.ToList();
            foreach (var key in keys)
            {
                _memoryCache.Remove(key);
            }
            
            _keyTracker.Clear();
            _itemCount = 0;
            
            _logger.LogInformation("Cleared all cache items. Removed {Count} keys", keys.Count);
            
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing cache");
            throw;
        }
    }

    public async Task<CacheStatistics> GetStatisticsAsync()
    {
        try
        {
            var stats = new CacheStatistics
            {
                HitCount = _hitCount,
                MissCount = _missCount,
                ItemCount = _keyTracker.Count,
                SizeInBytes = EstimateCacheSize()
            };
            
            return await Task.FromResult(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting cache statistics");
            return new CacheStatistics();
        }
    }

    private void CleanupExpiredKeys(object? state)
    {
        try
        {
            var keysToCheck = _keyTracker.Keys.ToList();
            foreach (var key in keysToCheck)
            {
                if (!_memoryCache.TryGetValue(key, out _))
                {
                    _keyTracker.TryRemove(key, out object? _);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during cleanup of expired keys");
        }
    }

    private long EstimateCacheSize()
    {
        // 简单估算缓存大小
        return _keyTracker.Count * 1024; // 假设每个缓存项平均1KB
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _cleanupTimer?.Dispose();
            _keyTracker.Clear();
            _disposed = true;
        }
    }
}