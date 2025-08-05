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

    /// <summary>
    /// Adds Making Entity Framework Core services with enhanced repositories supporting specifications, caching, and bulk operations.
    /// </summary>
    /// <typeparam name="TDbContext">The type of the DbContext.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="optionsAction">Configuration action for DbContext options.</param>
    /// <returns>The service collection for method chaining.</returns>
    public static IServiceCollection AddEnhancedMakingEntityFrameworkCore<TDbContext>(
        this IServiceCollection services,
        Action<DbContextOptionsBuilder> optionsAction)
        where TDbContext : MakingDbContext
    {
        // Add basic services
        services.AddMakingEntityFrameworkCore<TDbContext>(optionsAction);

        // Add memory cache if not already registered
        services.AddMemoryCache();

        // Replace basic repositories with enhanced versions
        services.AddScoped(typeof(IRepository<>), typeof(EnhancedEfCoreRepository<>));
        services.AddScoped(typeof(IRepository<,>), typeof(EnhancedEfCoreRepository<,>));

        // Register batch processor factory
        services.AddScoped(typeof(IBatchProcessorFactory<>), typeof(EnhancedEfCoreRepository<>));

        return services;
    }

    /// <summary>
    /// Adds Making Entity Framework Core services with enhanced repositories and custom configuration.
    /// </summary>
    /// <typeparam name="TDbContext">The type of the DbContext.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="optionsAction">Configuration action for DbContext options.</param>
    /// <param name="repositoryRegistration">Action to register custom repositories.</param>
    /// <returns>The service collection for method chaining.</returns>
    public static IServiceCollection AddEnhancedMakingEntityFrameworkCore<TDbContext>(
        this IServiceCollection services,
        Action<DbContextOptionsBuilder> optionsAction,
        Action<IServiceCollection> repositoryRegistration)
        where TDbContext : MakingDbContext
    {
        services.AddEnhancedMakingEntityFrameworkCore<TDbContext>(optionsAction);
        repositoryRegistration(services);
        return services;
    }

    /// <summary>
    /// Configuration options for enhanced Entity Framework Core features.
    /// </summary>
    public class EnhancedEfCoreOptions
    {
        /// <summary>
        /// Gets or sets whether to enable query result caching (default: true).
        /// </summary>
        public bool EnableCaching { get; set; } = true;

        /// <summary>
        /// Gets or sets the default cache expiration time (default: 30 minutes).
        /// </summary>
        public TimeSpan DefaultCacheExpiration { get; set; } = TimeSpan.FromMinutes(30);

        /// <summary>
        /// Gets or sets whether to enable batch operations (default: true).
        /// </summary>
        public bool EnableBatchOperations { get; set; } = true;

        /// <summary>
        /// Gets or sets the default batch size for bulk operations (default: 1000).
        /// </summary>
        public int DefaultBatchSize { get; set; } = 1000;

        /// <summary>
        /// Gets or sets whether to use transactions for batch operations (default: true).
        /// </summary>
        public bool UseBatchTransactions { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to enable soft delete filtering by default (default: true).
        /// </summary>
        public bool EnableSoftDeleteFilter { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to enable query splitting for complex includes (default: false).
        /// </summary>
        public bool EnableQuerySplitting { get; set; } = false;
    }

    /// <summary>
    /// Adds Making Entity Framework Core services with enhanced repositories and custom options.
    /// </summary>
    /// <typeparam name="TDbContext">The type of the DbContext.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="optionsAction">Configuration action for DbContext options.</param>
    /// <param name="enhancedOptionsAction">Configuration action for enhanced features.</param>
    /// <returns>The service collection for method chaining.</returns>
    public static IServiceCollection AddEnhancedMakingEntityFrameworkCore<TDbContext>(
        this IServiceCollection services,
        Action<DbContextOptionsBuilder> optionsAction,
        Action<EnhancedEfCoreOptions>? enhancedOptionsAction = null)
        where TDbContext : MakingDbContext
    {
        var enhancedOptions = new EnhancedEfCoreOptions();
        enhancedOptionsAction?.Invoke(enhancedOptions);

        // Configure services based on options
        services.AddSingleton(enhancedOptions);
        
        if (enhancedOptions.EnableCaching)
        {
            services.AddMemoryCache();
        }

        // Add basic services
        services.AddMakingEntityFrameworkCore<TDbContext>(optionsAction);

        // Replace basic repositories with enhanced versions
        services.AddScoped(typeof(IRepository<>), typeof(EnhancedEfCoreRepository<>));
        services.AddScoped(typeof(IRepository<,>), typeof(EnhancedEfCoreRepository<,>));

        if (enhancedOptions.EnableBatchOperations)
        {
            services.AddScoped(typeof(IBatchProcessorFactory<>), typeof(EnhancedEfCoreRepository<>));
            services.AddScoped(typeof(IBatchProcessor<>), typeof(EfCoreBatchProcessor<>));
        }

        return services;
    }

    /// <summary>
    /// Registers an enhanced repository implementation.
    /// </summary>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    /// <typeparam name="TRepository">The repository implementation type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for method chaining.</returns>
    public static IServiceCollection AddEnhancedRepository<TEntity, TRepository>(this IServiceCollection services)
        where TEntity : class, Making.Ddd.Domain.Domain.Entities.IEntity
        where TRepository : class, IRepository<TEntity>
    {
        services.AddScoped<IRepository<TEntity>, TRepository>();
        
        // If the repository supports batch processing, register it
        if (typeof(IBatchProcessorFactory<TEntity>).IsAssignableFrom(typeof(TRepository)))
        {
            services.AddScoped<IBatchProcessorFactory<TEntity>>(provider => 
                (IBatchProcessorFactory<TEntity>)provider.GetRequiredService<IRepository<TEntity>>());
        }
        
        return services;
    }

    /// <summary>
    /// Registers an enhanced repository implementation with typed key.
    /// </summary>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    /// <typeparam name="TKey">The key type.</typeparam>
    /// <typeparam name="TRepository">The repository implementation type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for method chaining.</returns>
    public static IServiceCollection AddEnhancedRepository<TEntity, TKey, TRepository>(this IServiceCollection services)
        where TEntity : class, Making.Ddd.Domain.Domain.Entities.IEntity<TKey>
        where TRepository : class, IRepository<TEntity, TKey>
    {
        services.AddScoped<IRepository<TEntity, TKey>, TRepository>();
        
        // If the repository supports batch processing, register it
        if (typeof(IBatchProcessorFactory<TEntity>).IsAssignableFrom(typeof(TRepository)))
        {
            services.AddScoped<IBatchProcessorFactory<TEntity>>(provider => 
                (IBatchProcessorFactory<TEntity>)provider.GetRequiredService<IRepository<TEntity, TKey>>());
        }
        
        return services;
    }
}