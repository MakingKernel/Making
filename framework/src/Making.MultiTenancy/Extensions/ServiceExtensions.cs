using Making.MultiTenancy.Abstractions.MultiTenancy;
using Making.MultiTenancy.MultiTenancy;
using Microsoft.Extensions.DependencyInjection;

namespace Making.MultiTenancy.Extensions;

public static class ServiceExtensions
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="services"></param>
    /// <returns></returns>
    public static IServiceCollection AddMultiTenancyServices(this IServiceCollection services)
    {
        services.AddSingleton<ICurrentTenantAccessor>(AsyncLocalCurrentTenantAccessor.Instance);

        return services;
    }
}