namespace Mark.MemoryCache.MemoryCache;

/// <summary>
/// 内存缓存服务接口
/// </summary>
public interface IMemoryCacheService
{
    /// <summary>
    /// 设置缓存
    /// </summary>
    /// <typeparam name="T">缓存值类型</typeparam>
    /// <param name="key">缓存键</param>
    /// <param name="value">缓存值</param>
    /// <param name="expiration">过期时间</param>
    Task SetAsync<T>(string key, T value, TimeSpan? expiration = null);

    /// <summary>
    /// 获取缓存
    /// </summary>
    /// <typeparam name="T">缓存值类型</typeparam>
    /// <param name="key">缓存键</param>
    /// <returns>缓存值</returns>
    Task<T?> GetAsync<T>(string key);

    /// <summary>
    /// 获取或设置缓存
    /// </summary>
    /// <typeparam name="T">缓存值类型</typeparam>
    /// <param name="key">缓存键</param>
    /// <param name="factory">缓存值工厂方法</param>
    /// <param name="expiration">过期时间</param>
    /// <returns>缓存值</returns>
    Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiration = null);

    /// <summary>
    /// 删除缓存
    /// </summary>
    /// <param name="key">缓存键</param>
    Task RemoveAsync(string key);

    /// <summary>
    /// 批量删除缓存（支持模式匹配）
    /// </summary>
    /// <param name="pattern">缓存键模式</param>
    Task RemoveByPatternAsync(string pattern);

    /// <summary>
    /// 检查缓存是否存在
    /// </summary>
    /// <param name="key">缓存键</param>
    /// <returns>是否存在</returns>
    Task<bool> ExistsAsync(string key);

    /// <summary>
    /// 刷新缓存过期时间
    /// </summary>
    /// <param name="key">缓存键</param>
    /// <param name="expiration">新的过期时间</param>
    Task RefreshAsync(string key, TimeSpan expiration);

    /// <summary>
    /// 获取所有缓存键
    /// </summary>
    /// <param name="pattern">键模式（可选）</param>
    /// <returns>缓存键列表</returns>
    Task<IEnumerable<string>> GetKeysAsync(string? pattern = null);

    /// <summary>
    /// 清空所有缓存
    /// </summary>
    Task ClearAsync();

    /// <summary>
    /// 获取缓存统计信息
    /// </summary>
    /// <returns>缓存统计</returns>
    Task<CacheStatistics> GetStatisticsAsync();
}