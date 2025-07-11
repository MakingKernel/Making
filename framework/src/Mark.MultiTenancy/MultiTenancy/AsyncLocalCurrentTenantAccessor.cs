using Mark.MultiTenancy.Abstractions.MultiTenancy;

namespace Mark.MultiTenancy.MultiTenancy;

public class AsyncLocalCurrentTenantAccessor : ICurrentTenantAccessor
{
    public static AsyncLocalCurrentTenantAccessor Instance { get; } = new();

    public BasicTenantInfo? Current {
        get => _currentScope.Value;
        set => _currentScope.Value = value;
    }

    private readonly AsyncLocal<BasicTenantInfo?> _currentScope;

    private AsyncLocalCurrentTenantAccessor()
    {
        _currentScope = new AsyncLocal<BasicTenantInfo?>();
    }
}
