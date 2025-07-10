using Mapster;
using MapsterMapper;
using Microsoft.Extensions.DependencyInjection;

namespace Mark.Mapster.Extensions;

public static class ServiceExtensions
{
    public static IServiceCollection AddMapster(this IServiceCollection services,
        Action<IServiceProvider, TypeAdapterConfig>? config = null)
    {
        services.AddSingleton<IMapper>((provider =>
        {
            var globalSettings = TypeAdapterConfig.GlobalSettings;

            config?.Invoke(provider, globalSettings);

            return new Mapper(globalSettings);
        }));
        return services;
    }
}