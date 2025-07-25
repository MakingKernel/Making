namespace Making.MultiTenancy.Abstractions.MultiTenancy;

public interface ICurrentTenant
{
    bool IsAvailable { get; }

    Guid? Id { get; }

    string? Name { get; }
}