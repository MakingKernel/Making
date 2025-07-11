using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Caching.Memory;
using Mark.MemoryCache.MemoryCache;

namespace Mark.MemoryCache.Extensions;

/// <summary>
/// 内存缓存服务扩展
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// 添加内存缓存服务
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection AddMarkMemoryCache(this IServiceCollection services)
    {
        services.AddMemoryCache();
        services.AddSingleton<IMemoryCacheService, MemoryCacheService>();
        
        return services;
    }
    
    /// <summary>
    /// 添加内存缓存服务（带配置）
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="configure">配置委托</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection AddMarkMemoryCache(this IServiceCollection services, Action<MemoryCacheOptions> configure)
    {
        services.AddMemoryCache(configure);
        services.AddSingleton<IMemoryCacheService, MemoryCacheService>();
        
        return services;
    }
}
