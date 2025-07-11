namespace Mark.MemoryCache.MemoryCache;

/// <summary>
/// 缓存项
/// </summary>
internal class CacheItem<T>
{
    public T Value { get; set; } = default!;
    public DateTime CreatedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public bool IsExpired => ExpiresAt.HasValue && DateTime.UtcNow > ExpiresAt.Value;
}

/// <summary>
/// 缓存变更通知
/// </summary>
public class CacheChangeNotification
{
    public string Key { get; set; } = string.Empty;
    public CacheOperation Operation { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// 缓存操作类型
/// </summary>
public enum CacheOperation
{
    Set,
    Remove,
    Clear,
    Refresh
}
