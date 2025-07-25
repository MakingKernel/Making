using Making.MemoryCache.MemoryCache;
using Making.MemoryCache.Redis.Options;
using Making.MemoryCache.Redis.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using StackExchange.Redis;

namespace Making.MemoryCache.Redis.Extensions;

/// <summary>
/// Redis缓存服务扩展
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// 添加Redis缓存服务
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="connectionString">Redis连接字符串</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection AddMakingRedisCache(this IServiceCollection services, string connectionString)
    {
        return services.AddMakingRedisCache(options =>
        {
            options.ConnectionString = connectionString;
        });
    }

    /// <summary>
    /// 添加Redis缓存服务
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="configure">配置委托</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection AddMakingRedisCache(this IServiceCollection services, Action<RedisCacheOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);

        services.Configure(configure);

        // 添加Redis连接
        services.AddSingleton<IConnectionMultiplexer>(provider =>
        {
            var options = provider.GetRequiredService<Microsoft.Extensions.Options.IOptions<RedisCacheOptions>>().Value;
            var config = ConfigurationOptions.Parse(options.ConnectionString);
            config.ConnectRetry = options.ConnectRetry;
            config.ConnectTimeout = options.ConnectTimeout;
            config.SyncTimeout = options.SyncTimeout;
            return ConnectionMultiplexer.Connect(config);
        });

        services.AddSingleton<IMemoryCacheService, RedisCacheService>();

        return services;
    }

    /// <summary>
    /// 添加多级缓存服务（内存 + Redis）
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="redisConnectionString">Redis连接字符串</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection AddMakingHybridCache(this IServiceCollection services, string redisConnectionString)
    {
        return services.AddMakingHybridCache(
            redisOptions => redisOptions.ConnectionString = redisConnectionString,
            hybridOptions => { });
    }

    /// <summary>
    /// 添加多级缓存服务（内存 + Redis）
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="configureRedis">Redis配置委托</param>
    /// <param name="configureHybrid">多级缓存配置委托</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection AddMakingHybridCache(
        this IServiceCollection services,
        Action<RedisCacheOptions> configureRedis,
        Action<HybridCacheOptions> configureHybrid)
    {
        ArgumentNullException.ThrowIfNull(configureRedis);
        ArgumentNullException.ThrowIfNull(configureHybrid);

        // 配置选项
        services.Configure(configureRedis);
        services.Configure(configureHybrid);

        // 添加Redis连接
        services.AddSingleton<IConnectionMultiplexer>(provider =>
        {
            var options = provider.GetRequiredService<Microsoft.Extensions.Options.IOptions<RedisCacheOptions>>().Value;
            var config = ConfigurationOptions.Parse(options.ConnectionString);
            config.ConnectRetry = options.ConnectRetry;
            config.ConnectTimeout = options.ConnectTimeout;
            config.SyncTimeout = options.SyncTimeout;
            return ConnectionMultiplexer.Connect(config);
        });

        // 添加本地内存缓存
        services.AddMemoryCache();
        services.AddSingleton<MemoryCacheService>();

        // 添加Redis缓存服务
        services.AddSingleton<RedisCacheService>();

        // 添加多级缓存服务
        services.AddSingleton<IMemoryCacheService>(provider =>
        {
            var localCache = provider.GetRequiredService<MemoryCacheService>();
            var redisCache = provider.GetRequiredService<RedisCacheService>();
            var hybridOptions = provider.GetRequiredService<Microsoft.Extensions.Options.IOptions<HybridCacheOptions>>();
            var logger = provider.GetRequiredService<Microsoft.Extensions.Logging.ILogger<HybridCacheService>>();
            var connectionMultiplexer = provider.GetRequiredService<IConnectionMultiplexer>();

            return new HybridCacheService(localCache, redisCache, hybridOptions, logger, connectionMultiplexer);
        });

        // 注册为后台服务
        services.AddSingleton<IHostedService>(provider => 
            (HybridCacheService)provider.GetRequiredService<IMemoryCacheService>());

        return services;
    }

    /// <summary>
    /// 添加分布式缓存服务（仅Redis）
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="connectionString">Redis连接字符串</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection AddMakingDistributedCache(this IServiceCollection services, string connectionString)
    {
        return services.AddMakingDistributedCache(options =>
        {
            options.ConnectionString = connectionString;
        });
    }

    /// <summary>
    /// 添加分布式缓存服务（仅Redis）
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="configure">配置委托</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection AddMakingDistributedCache(this IServiceCollection services, Action<RedisCacheOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);

        services.Configure(configure);

        // 添加Redis连接
        services.AddSingleton<IConnectionMultiplexer>(provider =>
        {
            var options = provider.GetRequiredService<Microsoft.Extensions.Options.IOptions<RedisCacheOptions>>().Value;
            var config = ConfigurationOptions.Parse(options.ConnectionString);
            config.ConnectRetry = options.ConnectRetry;
            config.ConnectTimeout = options.ConnectTimeout;
            config.SyncTimeout = options.SyncTimeout;
            return ConnectionMultiplexer.Connect(config);
        });

        // 只注册Redis缓存服务
        services.AddSingleton<IMemoryCacheService, RedisCacheService>();

        return services;
    }
}
