using Making.MemoryCache.MemoryCache;
using Making.MemoryCache.Redis.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using System.Text.Json;
using System.IO.Compression;
using System.Text;

namespace Making.MemoryCache.Redis.Services;

/// <summary>
/// Redis缓存服务
/// </summary>
[Singleton]
public class RedisCacheService : IMemoryCacheService, IDisposable
{
    private readonly RedisCacheOptions _options;
    private readonly ILogger<RedisCacheService> _logger;
    private readonly IConnectionMultiplexer _connectionMultiplexer;
    private readonly IDatabase _database;
    private long _hitCount;
    private long _missCount;
    private long _itemCount;
    private readonly JsonSerializerOptions _jsonOptions;
    private bool _disposed;

    public RedisCacheService(
        IOptions<RedisCacheOptions> options,
        ILogger<RedisCacheService> logger,
        IConnectionMultiplexer connectionMultiplexer)
    {
        _options = options.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _connectionMultiplexer = connectionMultiplexer ?? throw new ArgumentNullException(nameof(connectionMultiplexer));
        _database = _connectionMultiplexer.GetDatabase(_options.Database);
        
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null)
    {
        ArgumentNullException.ThrowIfNull(key);

        try
        {
            var fullKey = GetFullKey(key);
            var jsonValue = JsonSerializer.Serialize(value, _jsonOptions);
            var bytes = Encoding.UTF8.GetBytes(jsonValue);

            // 数据压缩
            if (_options.EnableCompression && bytes.Length > _options.CompressionThreshold)
            {
                bytes = await CompressAsync(bytes);
                fullKey += ":compressed";
            }

            var exp = expiration ?? _options.DefaultExpiration;
            await _database.StringSetAsync(fullKey, bytes, exp);

            Interlocked.Increment(ref _itemCount);
            _logger.LogDebug("Set Redis cache key: {Key}, expiration: {Expiration}", key, exp);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting Redis cache key: {Key}", key);
            throw;
        }
    }

    public async Task<T?> GetAsync<T>(string key)
    {
        ArgumentNullException.ThrowIfNull(key);

        try
        {
            var fullKey = GetFullKey(key);
            var compressedKey = fullKey + ":compressed";

            // 首先尝试获取压缩版本
            var value = await _database.StringGetAsync(compressedKey);
            var isCompressed = value.HasValue;

            if (!isCompressed)
            {
                value = await _database.StringGetAsync(fullKey);
            }

            if (value.HasValue)
            {
                var bytes = value;
                if (isCompressed)
                {
                    bytes = await DecompressAsync(bytes);
                    if (bytes.HasValue) 
                    {
                        Interlocked.Increment(ref _missCount);
                        _logger.LogWarning("Failed to decompress data for key: {Key}", key);
                        return default;
                    }
                }

                var jsonValue = Encoding.UTF8.GetString(bytes!);
                var result = JsonSerializer.Deserialize<T>(jsonValue, _jsonOptions);

                Interlocked.Increment(ref _hitCount);
                _logger.LogDebug("Redis cache hit for key: {Key}", key);
                return result;
            }

            Interlocked.Increment(ref _missCount);
            _logger.LogDebug("Redis cache miss for key: {Key}", key);
            return default(T);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting Redis cache key: {Key}", key);
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
            _logger.LogError(ex, "Error in Redis GetOrSetAsync for key: {Key}", key);
            throw;
        }
    }

    public async Task RemoveAsync(string key)
    {
        ArgumentNullException.ThrowIfNull(key);

        try
        {
            var fullKey = GetFullKey(key);
            var compressedKey = fullKey + ":compressed";

            var tasks = new[]
            {
                _database.KeyDeleteAsync(fullKey),
                _database.KeyDeleteAsync(compressedKey)
            };

            await Task.WhenAll(tasks);
            Interlocked.Decrement(ref _itemCount);

            _logger.LogDebug("Removed Redis cache key: {Key}", key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing Redis cache key: {Key}", key);
            throw;
        }
    }

    public async Task RemoveByPatternAsync(string pattern)
    {
        ArgumentNullException.ThrowIfNull(pattern);

        try
        {
            var fullPattern = GetFullKey(pattern);
            var server = _connectionMultiplexer.GetServer(_connectionMultiplexer.GetEndPoints().First());
            var keys = server.Keys(database: _options.Database, pattern: fullPattern).ToArray();

            if (keys.Length > 0)
            {
                await _database.KeyDeleteAsync(keys);
                Interlocked.Add(ref _itemCount, -keys.Length);
            }

            _logger.LogDebug("Removed {Count} Redis cache keys matching pattern: {Pattern}", keys.Length, pattern);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing Redis cache keys by pattern: {Pattern}", pattern);
            throw;
        }
    }

    public async Task<bool> ExistsAsync(string key)
    {
        ArgumentNullException.ThrowIfNull(key);

        try
        {
            var fullKey = GetFullKey(key);
            var compressedKey = fullKey + ":compressed";

            var exists = await _database.KeyExistsAsync(fullKey) || await _database.KeyExistsAsync(compressedKey);
            return exists;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if Redis cache key exists: {Key}", key);
            return false;
        }
    }

    public async Task RefreshAsync(string key, TimeSpan expiration)
    {
        ArgumentNullException.ThrowIfNull(key);

        try
        {
            var fullKey = GetFullKey(key);
            var compressedKey = fullKey + ":compressed";

            var refreshTasks = new[]
            {
                _database.KeyExpireAsync(fullKey, expiration),
                _database.KeyExpireAsync(compressedKey, expiration)
            };

            await Task.WhenAll(refreshTasks);
            _logger.LogDebug("Refreshed Redis cache key: {Key} with new expiration: {Expiration}", key, expiration);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing Redis cache key: {Key}", key);
            throw;
        }
    }

    public async Task<IEnumerable<string>> GetKeysAsync(string? pattern = null)
    {
        try
        {
            var searchPattern = string.IsNullOrEmpty(pattern) ? GetFullKey("*") : GetFullKey(pattern);
            var server = _connectionMultiplexer.GetServer(_connectionMultiplexer.GetEndPoints().First());
            var keys = server.Keys(database: _options.Database, pattern: searchPattern)
                .Select(key => key.ToString().Substring(_options.KeyPrefix.Length))
                .Where(key => !key.EndsWith(":compressed"))
                .ToList();

            return await Task.FromResult(keys);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting Redis cache keys with pattern: {Pattern}", pattern);
            return Enumerable.Empty<string>();
        }
    }

    public async Task ClearAsync()
    {
        try
        {
            var server = _connectionMultiplexer.GetServer(_connectionMultiplexer.GetEndPoints().First());
            var keys = server.Keys(database: _options.Database, pattern: GetFullKey("*")).ToArray();

            if (keys.Length > 0)
            {
                await _database.KeyDeleteAsync(keys);
            }

            _itemCount = 0;
            _logger.LogInformation("Cleared all Redis cache items. Removed {Count} keys", keys.Length);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing Redis cache");
            throw;
        }
    }

    public async Task<CacheStatistics> GetStatisticsAsync()
    {
        try
        {
            var info = await _connectionMultiplexer.GetDatabase().ExecuteAsync("INFO", "memory");
            var memoryInfo = info.ToString();

            var stats = new CacheStatistics
            {
                HitCount = _hitCount,
                MissCount = _missCount,
                ItemCount = await GetItemCountAsync(),
                SizeInBytes = ExtractMemoryUsage(memoryInfo)
            };

            return stats;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting Redis cache statistics");
            return new CacheStatistics();
        }
    }

    private async Task<long> GetItemCountAsync()
    {
        try
        {
            var server = _connectionMultiplexer.GetServer(_connectionMultiplexer.GetEndPoints().First());
            var keys = server.Keys(database: _options.Database, pattern: GetFullKey("*"));
            return await Task.FromResult(keys.LongCount(key => !key.ToString().EndsWith(":compressed")));
        }
        catch
        {
            return _itemCount;
        }
    }

    private string GetFullKey(string key) => $"{_options.KeyPrefix}{key}";

    private async Task<byte[]> CompressAsync(byte[] data)
    {
        using var output = new MemoryStream();
        using var gzip = new GZipStream(output, CompressionMode.Compress);
        await gzip.WriteAsync(data);
        await gzip.FlushAsync();
        return output.ToArray();
    }

    private async Task<byte[]> DecompressAsync(byte[] compressedData)
    {
        if (compressedData == null) throw new ArgumentNullException(nameof(compressedData));
        
        using var input = new MemoryStream(compressedData);
        using var gzip = new GZipStream(input, CompressionMode.Decompress);
        using var output = new MemoryStream();
        await gzip.CopyToAsync(output);
        return output.ToArray();
    }

    private long ExtractMemoryUsage(string memoryInfo)
    {
        try
        {
            var lines = memoryInfo.Split('\n');
            var usedMemoryLine = lines.FirstOrDefault(line => line.StartsWith("used_memory:"));
            if (usedMemoryLine != null)
            {
                var value = usedMemoryLine.Split(':')[1].Trim();
                return long.Parse(value);
            }
        }
        catch
        {
            // 忽略解析错误
        }
        return 0;
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _disposed = true;
        }
    }
}
