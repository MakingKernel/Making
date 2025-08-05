using Making.Ddd.Domain.Domain.Repositories;
using Making.Ddd.Domain.Domain.Uow;
using Making.EntityFrameworkCore.EntityFrameworkCore;
using Making.EntityFrameworkCore.Performance;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;

namespace Making.EntityFrameworkCore.Extensions;

/// <summary>
/// Extension methods for configuring Entity Framework Core in the service collection.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds Making Entity Framework Core services to the service collection.
    /// </summary>
    /// <typeparam name="TDbContext">The type of the DbContext.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="optionsAction">Configuration action for DbContext options.</param>
    /// <returns>The service collection for method chaining.</returns>
    public static IServiceCollection AddMakingEntityFrameworkCore<TDbContext>(
        this IServiceCollection services,
        Action<DbContextOptionsBuilder> optionsAction)
        where TDbContext : MakingDbContext
    {
        services.AddDbContext<TDbContext>(options => { optionsAction(options); });

        services.AddScoped<MakingDbContext>(provider => provider.GetRequiredService<TDbContext>());

        services.AddScoped<IUnitOfWork, EfCoreUnitOfWork>();
        services.AddScoped(typeof(IRepository<>), typeof(EfCoreRepository<>));
        services.AddScoped(typeof(IRepository<,>), typeof(EfCoreRepository<,>));

        return services;
    }
}