using Making.Localization.Contributors;
using Making.Localization.Localization;
using Making.Localization.Options;
using Making.Localization.Resources;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;

namespace Making.Localization.Extensions;

/// <summary>
/// Extension methods for configuring Making localization services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds Making localization services to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configureOptions">Optional configuration action.</param>
    /// <returns>The service collection for method chaining.</returns>
    public static IServiceCollection AddMakingLocalization(
        this IServiceCollection services,
        Action<MakingLocalizationOptions>? configureOptions = null)
    {
        // Configure options
        if (configureOptions != null)
        {
            services.Configure(configureOptions);
        }

        // Register core services
        services.AddSingleton<ILocalizationResourceManager, InMemoryLocalizationResourceManager>();
        services.AddTransient<IMakingStringLocalizer, MakingStringLocalizer>();
        services.AddTransient(typeof(IMakingStringLocalizer<>), typeof(MakingStringLocalizer<>));

        // Register as standard IStringLocalizer services for compatibility
        services.AddTransient<IStringLocalizer>(provider => provider.GetRequiredService<IMakingStringLocalizer>());
        services.AddTransient(typeof(IStringLocalizer<>), typeof(MakingStringLocalizer<>));

        return services;
    }

    /// <summary>
    /// Adds JSON file-based localization resource contributor.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="resourcesPath">Path to the localization resource files.</param>
    /// <param name="priority">Priority of this contributor (higher values = higher priority).</param>
    /// <returns>The service collection for method chaining.</returns>
    public static IServiceCollection AddJsonFileLocalizationContributor(
        this IServiceCollection services,
        string resourcesPath,
        int priority = 100)
    {
        services.AddSingleton<ILocalizationResourceContributor>(provider =>
        {
            var logger = provider.GetRequiredService<Microsoft.Extensions.Logging.ILogger<JsonFileLocalizationResourceContributor>>();
            var contributor = new JsonFileLocalizationResourceContributor(resourcesPath, logger);
            
            // Set priority through reflection or create a wrapper if needed
            return contributor;
        });

        return services;
    }

    /// <summary>
    /// Adds embedded resource-based localization resource contributor.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="resourceType">Type used to locate the embedded resources.</param>
    /// <param name="priority">Priority of this contributor (higher values = higher priority).</param>
    /// <returns>The service collection for method chaining.</returns>
    public static IServiceCollection AddEmbeddedLocalizationContributor(
        this IServiceCollection services,
        Type resourceType,
        int priority = 50)
    {
        services.AddSingleton<ILocalizationResourceContributor>(provider =>
        {
            var logger = provider.GetRequiredService<Microsoft.Extensions.Logging.ILogger<EmbeddedResourceLocalizationResourceContributor>>();
            return new EmbeddedResourceLocalizationResourceContributor(resourceType, logger);
        });

        return services;
    }

    /// <summary>
    /// Adds embedded resource-based localization resource contributor.
    /// </summary>
    /// <typeparam name="TResource">Type used to locate the embedded resources.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="priority">Priority of this contributor (higher values = higher priority).</param>
    /// <returns>The service collection for method chaining.</returns>
    public static IServiceCollection AddEmbeddedLocalizationContributor<TResource>(
        this IServiceCollection services,
        int priority = 50)
    {
        return services.AddEmbeddedLocalizationContributor(typeof(TResource), priority);
    }

    /// <summary>
    /// Adds a custom localization resource contributor.
    /// </summary>
    /// <typeparam name="TContributor">The contributor type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for method chaining.</returns>
    public static IServiceCollection AddLocalizationContributor<TContributor>(
        this IServiceCollection services)
        where TContributor : class, ILocalizationResourceContributor
    {
        services.AddSingleton<ILocalizationResourceContributor, TContributor>();
        return services;
    }

    /// <summary>
    /// Adds localization with default setup (JSON files and common configurations).
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="resourcesPath">Path to the localization resource files (default: "Resources/Localization").</param>
    /// <returns>The service collection for method chaining.</returns>
    public static IServiceCollection AddMakingLocalizationWithDefaults(
        this IServiceCollection services,
        string resourcesPath = "Resources/Localization")
    {
        services.AddMakingLocalization(options =>
        {
            options.DefaultCulture = "en";
            options.SupportedCultures = new[] { "en", "en-US", "zh", "zh-CN", "zh-TW" };
            options.FallbackToDefaultCulture = true;
        });

        services.AddJsonFileLocalizationContributor(resourcesPath);

        return services;
    }
}