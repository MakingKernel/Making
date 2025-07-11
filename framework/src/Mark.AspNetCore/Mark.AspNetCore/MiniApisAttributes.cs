namespace Mark.AspNetCore;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public sealed class IgnoreFilterAttribute : Attribute
{
}

/// <summary>
/// 指定端点过滤器的特性
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public sealed class FilterAttribute : Attribute
{
    /// <summary>
    /// 获取过滤器类型数组
    /// </summary>
    public Type[] Types { get; }

    /// <summary>
    /// 初始化FilterAttribute的新实例
    /// </summary>
    /// <param name="types">过滤器类型数组</param>
    /// <exception cref="ArgumentNullException">当types为null时抛出</exception>
    public FilterAttribute(params Type[] types)
    {
        Types = types ?? throw new ArgumentNullException(nameof(types));
    }
}

/// <summary>
/// 指示方法应被忽略，不生成API路由的特性
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class IgnoreRouteAttribute : Attribute
{
}

/// <summary>
/// 标注类需要自动生成MiniApi映射的特性
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public sealed class MiniApiAttribute : Attribute
{
    /// <summary>
    /// 获取或设置API路由前缀
    /// </summary>
    public string? Route { get; set; }
    
    /// <summary>
    /// 获取或设置API标签
    /// </summary>
    public string? Tags { get; set; }
    
    /// <summary>
    /// 初始化MiniApiAttribute的新实例
    /// </summary>
    public MiniApiAttribute()
    {
    }
    
    /// <summary>
    /// 初始化MiniApiAttribute的新实例
    /// </summary>
    /// <param name="route">API路由前缀</param>
    public MiniApiAttribute(string route)
    {
        Route = route;
    }
}