using Making.AspNetCore.Security;
using Making.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.DependencyInjection;

namespace Making.AspNetCore.Extensions;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds the Making claims transformation service to the ASP.NET Core dependency injection container.
    /// </summary>
    /// <param name="services"></param>
    /// <returns></returns>
    public static IServiceCollection AddMakingClaimsTransformation(this IServiceCollection services)
    {
        if (services.IsAdded<IClaimsTransformation>())
        {
            return services;
        }
        
        services.AddSingleton<ICurrentPrincipalAccessor, HttpContextCurrentPrincipalAccessor>();
        return services.AddTransient<IClaimsTransformation, MakingClaimsTransformation>();
    }


    public static bool IsAdded<T>(this IServiceCollection services)
    {
        return services.IsAdded(typeof(T));
    }

    public static bool IsAdded(this IServiceCollection services, Type type)
    {
        return services.Any(d => d.ServiceType == type);
    }
}