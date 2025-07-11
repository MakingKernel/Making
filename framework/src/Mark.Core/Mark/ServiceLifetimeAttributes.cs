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

/// <summary>
/// 自定义注册类型
/// </summary>
/// <param name="serviceType"></param>
public sealed class RegisterServiceAttribute(Type serviceType) : Attribute
{
    public Type ServiceType => serviceType;
}