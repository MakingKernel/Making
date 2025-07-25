
namespace Making.AspNetCore;

/// <summary>
/// 标记不需要结果包装的特性
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public class SkipWrapAttribute : Attribute { }