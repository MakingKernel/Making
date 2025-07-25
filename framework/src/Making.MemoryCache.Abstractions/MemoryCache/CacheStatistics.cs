namespace Making.MemoryCache.MemoryCache;

/// <summary>
/// 缓存统计信息
/// </summary>
public class CacheStatistics
{
    /// <summary>
    /// 缓存命中次数
    /// </summary>
    public long HitCount { get; set; }

    /// <summary>
    /// 缓存未命中次数
    /// </summary>
    public long MissCount { get; set; }

    /// <summary>
    /// 总缓存项数量
    /// </summary>
    public long ItemCount { get; set; }

    /// <summary>
    /// 缓存命中率
    /// </summary>
    public double HitRate => HitCount + MissCount > 0 ? (double)HitCount / (HitCount + MissCount) : 0;

    /// <summary>
    /// 缓存大小（字节）
    /// </summary>
    public long SizeInBytes { get; set; }
}
