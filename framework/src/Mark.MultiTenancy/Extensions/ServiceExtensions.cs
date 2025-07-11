using Mark.MultiTenancy.Abstractions.MultiTenancy;
using Mark.MultiTenancy.MultiTenancy;
using Microsoft.Extensions.DependencyInjection;

namespace Mark.MultiTenancy.Extensions;

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