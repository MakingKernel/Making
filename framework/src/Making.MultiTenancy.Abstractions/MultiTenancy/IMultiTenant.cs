namespace Making.MultiTenancy.Abstractions.MultiTenancy;

public interface IMultiTenant
{
    /// <summary>
    /// Id of the related tenant.
    /// </summary>
    Guid? TenantId { get; set; }
}
