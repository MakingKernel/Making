using System;

namespace Making;

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

/// <summary>
/// Factory method registration attribute
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public sealed class FactoryAttribute : Attribute
{
    public string MethodName { get; }
    
    public FactoryAttribute(string methodName)
    {
        MethodName = methodName;
    }
}

/// <summary>
/// Keyed service registration attribute
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public sealed class KeyedAttribute : Attribute
{
    public object Key { get; }
    
    public KeyedAttribute(object key)
    {
        Key = key;
    }
}

/// <summary>
/// Service decorator registration attribute
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public sealed class DecoratorAttribute : Attribute
{
    public Type ServiceType { get; }
    public int Order { get; set; } = 0;
    
    public DecoratorAttribute(Type serviceType)
    {
        ServiceType = serviceType;
    }
}

/// <summary>
/// Conditional service registration attribute
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public sealed class ConditionalAttribute : Attribute
{
    public string Condition { get; }
    
    public ConditionalAttribute(string condition)
    {
        Condition = condition;
    }
}

/// <summary>
/// Open generic service registration attribute
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public sealed class OpenGenericAttribute : Attribute
{
}

/// <summary>
/// Multiple implementation support attribute
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public sealed class MultipleAttribute : Attribute
{
    public bool ReplaceExisting { get; set; } = false;
}

/// <summary>
/// Hosted service registration attribute
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public sealed class HostedServiceAttribute : Attribute
{
}

/// <summary>
/// Service configuration attribute
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public sealed class ConfigureAttribute : Attribute
{
    public string ConfigurationKey { get; }
    
    public ConfigureAttribute(string configurationKey)
    {
        ConfigurationKey = configurationKey;
    }
}

/// <summary>
/// Priority for service registration order
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public sealed class PriorityAttribute : Attribute
{
    public int Order { get; }
    
    public PriorityAttribute(int order)
    {
        Order = order;
    }
}