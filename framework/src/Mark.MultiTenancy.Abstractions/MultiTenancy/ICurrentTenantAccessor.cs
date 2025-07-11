namespace Mark.MultiTenancy.Abstractions.MultiTenancy;

public interface ICurrentTenantAccessor
{
    BasicTenantInfo? Current { get; set; }
}