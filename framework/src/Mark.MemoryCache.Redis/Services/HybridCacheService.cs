using Mark.MemoryCache.MemoryCache;
using Mark.MemoryCache.Redis.Options;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using System.Text.Json;

namespace Mark.MemoryCache.Redis.Services;

/// <summary>
/// 多级缓存服务（内存 + Redis）
/// </summary>
[Singleton]
public class HybridCacheService : IMemoryCacheService, IHostedService, IDisposable
{
    private readonly IMemoryCacheService _localCache;
    private readonly IMemoryCacheService _redisCache;
    private readonly HybridCacheOptions _options;
    private readonly ILogger<HybridCacheService> _logger;
    private readonly IConnectionMultiplexer _connectionMultiplexer;
    private readonly ISubscriber _subscriber;
    private long _hitCount;
    private long _missCount;
    private long _itemCount;
    private bool _disposed;

    public HybridCacheService(
        IMemoryCacheService localCache,
        IMemoryCacheService redisCache,
        IOptions<HybridCacheOptions> options,
        ILogger<HybridCacheService> logger,
        IConnectionMultiplexer connectionMultiplexer)
    {
        _localCache = localCache ?? throw new ArgumentNullException(nameof(localCache));
        _redisCache = redisCache ?? throw new ArgumentNullException(nameof(redisCache));
        _options = options.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _connectionMultiplexer = connectionMultiplexer ?? throw new ArgumentNullException(nameof(connectionMultiplexer));
        _subscriber = _connectionMultiplexer.GetSubscriber();
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (_options.EnablePubSubSync)
        {
            await _subscriber.SubscribeAsync(RedisChannel.Literal(_options.SyncChannelName), OnCacheSyncMessage);
            _logger.LogInformation("Subscribed to cache sync channel: {Channel}", _options.SyncChannelName);
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_options.EnablePubSubSync)
        {
            await _subscriber.UnsubscribeAsync(RedisChannel.Literal(_options.SyncChannelName));
            _logger.LogInformation("Unsubscribed from cache sync channel: {Channel}", _options.SyncChannelName);
        }
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null)
    {
        ArgumentNullException.ThrowIfNull(key);

        try
        {
            // 设置到Redis缓存
            await _redisCache.SetAsync(key, value, expiration);

            // 设置到本地缓存（使用较短的过期时间）
            if (_options.EnableLocalCache)
            {
                var localExpiration = expiration.HasValue 
                    ? TimeSpan.FromMilliseconds(Math.Min(expiration.Value.TotalMilliseconds, _options.LocalCacheDefaultExpiration.TotalMilliseconds))
                    : _options.LocalCacheDefaultExpiration;
                
                await _localCache.SetAsync(key, value, localExpiration);
            }

            // 发布同步消息
            if (_options.EnablePubSubSync)
            {
                await PublishSyncMessage(key, CacheOperation.Set);
            }

            Interlocked.Increment(ref _itemCount);
            _logger.LogDebug("Set hybrid cache key: {Key}, expiration: {Expiration}", key, expiration);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting hybrid cache key: {Key}", key);
            throw;
        }
    }

    public async Task<T?> GetAsync<T>(string key)
    {
        ArgumentNullException.ThrowIfNull(key);

        try
        {
            // 首先尝试从本地缓存获取
            if (_options.EnableLocalCache)
            {
                var localValue = await _localCache.GetAsync<T>(key);
                if (localValue != null)
                {
                    Interlocked.Increment(ref _hitCount);
                    _logger.LogDebug("Local cache hit for key: {Key}", key);
                    return localValue;
                }
            }

            // 从Redis缓存获取
            var redisValue = await _redisCache.GetAsync<T>(key);
            if (redisValue != null)
            {
                // 回写到本地缓存
                if (_options.EnableLocalCache)
                {
                    await _localCache.SetAsync(key, redisValue, _options.LocalCacheDefaultExpiration);
                }

                Interlocked.Increment(ref _hitCount);
                _logger.LogDebug("Redis cache hit for key: {Key}", key);
                return redisValue;
            }

            Interlocked.Increment(ref _missCount);
            _logger.LogDebug("Cache miss for key: {Key}", key);
            return default(T);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting hybrid cache key: {Key}", key);
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
            _logger.LogError(ex, "Error in hybrid GetOrSetAsync for key: {Key}", key);
            throw;
        }
    }

    public async Task RemoveAsync(string key)
    {
        ArgumentNullException.ThrowIfNull(key);

        try
        {
            // 从Redis和本地缓存中删除
            var tasks = new List<Task>
            {
                _redisCache.RemoveAsync(key)
            };

            if (_options.EnableLocalCache)
            {
                tasks.Add(_localCache.RemoveAsync(key));
            }

            await Task.WhenAll(tasks);

            // 发布同步消息
            if (_options.EnablePubSubSync)
            {
                await PublishSyncMessage(key, CacheOperation.Remove);
            }

            Interlocked.Decrement(ref _itemCount);
            _logger.LogDebug("Removed hybrid cache key: {Key}", key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing hybrid cache key: {Key}", key);
            throw;
        }
    }

    public async Task RemoveByPatternAsync(string pattern)
    {
        ArgumentNullException.ThrowIfNull(pattern);

        try
        {
            var tasks = new List<Task>
            {
                _redisCache.RemoveByPatternAsync(pattern)
            };

            if (_options.EnableLocalCache)
            {
                tasks.Add(_localCache.RemoveByPatternAsync(pattern));
            }

            await Task.WhenAll(tasks);

            // 发布同步消息
            if (_options.EnablePubSubSync)
            {
                await PublishSyncMessage(pattern, CacheOperation.Remove);
            }

            _logger.LogDebug("Removed hybrid cache keys matching pattern: {Pattern}", pattern);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing hybrid cache keys by pattern: {Pattern}", pattern);
            throw;
        }
    }

    public async Task<bool> ExistsAsync(string key)
    {
        ArgumentNullException.ThrowIfNull(key);

        try
        {
            // 检查本地缓存
            if (_options.EnableLocalCache && await _localCache.ExistsAsync(key))
            {
                return true;
            }

            // 检查Redis缓存
            return await _redisCache.ExistsAsync(key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if hybrid cache key exists: {Key}", key);
            return false;
        }
    }

    public async Task RefreshAsync(string key, TimeSpan expiration)
    {
        ArgumentNullException.ThrowIfNull(key);

        try
        {
            var tasks = new List<Task>
            {
                _redisCache.RefreshAsync(key, expiration)
            };

            if (_options.EnableLocalCache)
            {
                var localExpiration = TimeSpan.FromMilliseconds(Math.Min(expiration.TotalMilliseconds, _options.LocalCacheDefaultExpiration.TotalMilliseconds));
                tasks.Add(_localCache.RefreshAsync(key, localExpiration));
            }

            await Task.WhenAll(tasks);

            // 发布同步消息
            if (_options.EnablePubSubSync)
            {
                await PublishSyncMessage(key, CacheOperation.Refresh);
            }

            _logger.LogDebug("Refreshed hybrid cache key: {Key} with new expiration: {Expiration}", key, expiration);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing hybrid cache key: {Key}", key);
            throw;
        }
    }

    public async Task<IEnumerable<string>> GetKeysAsync(string? pattern = null)
    {
        try
        {
            // 优先从Redis获取，因为它包含所有数据
            return await _redisCache.GetKeysAsync(pattern);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting hybrid cache keys with pattern: {Pattern}", pattern);
            return Enumerable.Empty<string>();
        }
    }

    public async Task ClearAsync()
    {
        try
        {
            var tasks = new List<Task>
            {
                _redisCache.ClearAsync()
            };

            if (_options.EnableLocalCache)
            {
                tasks.Add(_localCache.ClearAsync());
            }

            await Task.WhenAll(tasks);

            // 发布同步消息
            if (_options.EnablePubSubSync)
            {
                await PublishSyncMessage("*", CacheOperation.Clear);
            }

            _itemCount = 0;
            _logger.LogInformation("Cleared all hybrid cache items");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing hybrid cache");
            throw;
        }
    }

    public async Task<CacheStatistics> GetStatisticsAsync()
    {
        try
        {
            var redisStats = await _redisCache.GetStatisticsAsync();
            var localStats = _options.EnableLocalCache ? await _localCache.GetStatisticsAsync() : new CacheStatistics();

            var combinedStats = new CacheStatistics
            {
                HitCount = _hitCount,
                MissCount = _missCount,
                ItemCount = redisStats.ItemCount,
                SizeInBytes = redisStats.SizeInBytes + localStats.SizeInBytes
            };

            return combinedStats;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting hybrid cache statistics");
            return new CacheStatistics();
        }
    }

    private async Task PublishSyncMessage(string key, CacheOperation operation)
    {
        try
        {
            var message = new CacheChangeNotification
            {
                Key = key,
                Operation = operation,
                Timestamp = DateTime.UtcNow
            };

            var jsonMessage = JsonSerializer.Serialize(message);
            await _subscriber.PublishAsync(RedisChannel.Literal(_options.SyncChannelName), jsonMessage);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to publish cache sync message for key: {Key}", key);
        }
    }

    private async void OnCacheSyncMessage(RedisChannel channel, RedisValue message)
    {
        try
        {
            var notification = JsonSerializer.Deserialize<CacheChangeNotification>(message.ToString());
            if (notification == null) return;

            // 忽略自己发出的消息（可以通过NodeId区分）
            _logger.LogDebug("Received cache sync message: {Operation} for key: {Key}", notification.Operation, notification.Key);

            // 根据操作类型同步本地缓存
            if (_options.EnableLocalCache)
            {
                switch (notification.Operation)
                {
                    case CacheOperation.Remove:
                        if (notification.Key.Contains("*"))
                        {
                            await _localCache.RemoveByPatternAsync(notification.Key);
                        }
                        else
                        {
                            await _localCache.RemoveAsync(notification.Key);
                        }
                        break;
                    case CacheOperation.Clear:
                        await _localCache.ClearAsync();
                        break;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing cache sync message");
        }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            if (_localCache is IDisposable localDisposable)
                localDisposable.Dispose();
            
            if (_redisCache is IDisposable redisDisposable)
                redisDisposable.Dispose();
            
            _disposed = true;
        }
    }
}
