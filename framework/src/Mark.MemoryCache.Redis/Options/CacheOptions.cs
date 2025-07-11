namespace Mark.MemoryCache.Redis.Options;

/// <summary>
/// Redis缓存配置选项
/// </summary>
public class RedisCacheOptions
{
    /// <summary>
    /// Redis连接字符串
    /// </summary>
    public string ConnectionString { get; set; } = "localhost:6379";

    /// <summary>
    /// 数据库索引
    /// </summary>
    public int Database { get; set; } = 0;

    /// <summary>
    /// 键前缀
    /// </summary>
    public string KeyPrefix { get; set; } = "mark:cache:";

    /// <summary>
    /// 默认过期时间
    /// </summary>
    public TimeSpan DefaultExpiration { get; set; } = TimeSpan.FromHours(1);

    /// <summary>
    /// 启用数据压缩
    /// </summary>
    public bool EnableCompression { get; set; } = true;

    /// <summary>
    /// 压缩阈值（字节）
    /// </summary>
    public int CompressionThreshold { get; set; } = 1024;

    /// <summary>
    /// 连接重试次数
    /// </summary>
    public int ConnectRetry { get; set; } = 3;

    /// <summary>
    /// 连接超时时间（毫秒）
    /// </summary>
    public int ConnectTimeout { get; set; } = 5000;

    /// <summary>
    /// 同步超时时间（毫秒）
    /// </summary>
    public int SyncTimeout { get; set; } = 5000;
}

/// <summary>
/// 多级缓存配置选项
/// </summary>
public class HybridCacheOptions
{
    /// <summary>
    /// 启用本地缓存
    /// </summary>
    public bool EnableLocalCache { get; set; } = true;

    /// <summary>
    /// 本地缓存最大项数
    /// </summary>
    public int LocalCacheMaxItems { get; set; } = 1000;

    /// <summary>
    /// 本地缓存默认过期时间
    /// </summary>
    public TimeSpan LocalCacheDefaultExpiration { get; set; } = TimeSpan.FromMinutes(30);

    /// <summary>
    /// 启用Redis发布订阅同步
    /// </summary>
    public bool EnablePubSubSync { get; set; } = true;

    /// <summary>
    /// 同步通道名称
    /// </summary>
    public string SyncChannelName { get; set; } = "mark:cache:sync";

    /// <summary>
    /// 节点标识
    /// </summary>
    public string NodeId { get; set; } = Environment.MachineName;
}
