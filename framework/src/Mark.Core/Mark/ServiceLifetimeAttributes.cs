namespace Mark;

/// <summary>
/// 标记单例生命周期服务。
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class SingletonAttribute : Attribute { }

/// <summary>
/// 标记作用域生命周期服务。
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class ScopedAttribute : Attribute { }

/// <summary>
/// 标记瞬态生命周期服务。
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class TransientAttribute : Attribute { } 