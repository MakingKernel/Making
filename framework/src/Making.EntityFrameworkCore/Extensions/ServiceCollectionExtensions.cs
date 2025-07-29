using Making.Ddd.Domain.Domain.Repositories;
using Making.Ddd.Domain.Domain.Uow;
using Making.EntityFrameworkCore.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
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
        services.AddDbContext<TDbContext>(options =>
        {
            optionsAction(options);
        });

        services.AddScoped<MakingDbContext>(provider => provider.GetRequiredService<TDbContext>());

        services.AddScoped<IUnitOfWork, EfCoreUnitOfWork>();
        services.AddScoped(typeof(IRepository<>), typeof(EfCoreRepository<>));
        services.AddScoped(typeof(IRepository<,>), typeof(EfCoreRepository<,>));

        return services;
    }

    /// <summary>
    /// Adds Making Entity Framework Core services with custom repository implementations.
    /// </summary>
    /// <typeparam name="TDbContext">The type of the DbContext.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="optionsAction">Configuration action for DbContext options.</param>
    /// <param name="repositoryRegistration">Action to register custom repositories.</param>
    /// <returns>The service collection for method chaining.</returns>
    public static IServiceCollection AddMakingEntityFrameworkCore<TDbContext>(
        this IServiceCollection services,
        Action<DbContextOptionsBuilder> optionsAction,
        Action<IServiceCollection> repositoryRegistration)
        where TDbContext : MakingDbContext
    {
        services.AddMakingEntityFrameworkCore<TDbContext>(optionsAction);
        repositoryRegistration(services);
        return services;
    }

    /// <summary>
    /// Registers a custom repository implementation.
    /// </summary>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    /// <typeparam name="TRepository">The repository implementation type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for method chaining.</returns>
    public static IServiceCollection AddRepository<TEntity, TRepository>(this IServiceCollection services)
        where TEntity : class, Making.Ddd.Domain.Domain.Entities.IEntity
        where TRepository : class, IRepository<TEntity>
    {
        services.AddScoped<IRepository<TEntity>, TRepository>();
        return services;
    }

    /// <summary>
    /// Registers a custom repository implementation with typed key.
    /// </summary>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    /// <typeparam name="TKey">The key type.</typeparam>
    /// <typeparam name="TRepository">The repository implementation type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for method chaining.</returns>
    public static IServiceCollection AddRepository<TEntity, TKey, TRepository>(this IServiceCollection services)
        where TEntity : class, Making.Ddd.Domain.Domain.Entities.IEntity<TKey>
        where TRepository : class, IRepository<TEntity, TKey>
    {
        services.AddScoped<IRepository<TEntity, TKey>, TRepository>();
        return services;
    }
}