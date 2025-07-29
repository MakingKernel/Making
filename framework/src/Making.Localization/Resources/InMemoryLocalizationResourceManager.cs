using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Globalization;

namespace Making.Localization.Resources;

/// <summary>
/// In-memory implementation of localization resource manager.
/// </summary>
public class InMemoryLocalizationResourceManager : ILocalizationResourceManager
{
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, string>> _resources;
    private readonly IEnumerable<ILocalizationResourceContributor> _contributors;
    private readonly ILogger<InMemoryLocalizationResourceManager> _logger;
    private readonly object _lock = new object();
    private volatile bool _isInitialized = false;

    public InMemoryLocalizationResourceManager(
        IEnumerable<ILocalizationResourceContributor> contributors,
        ILogger<InMemoryLocalizationResourceManager> logger)
    {
        _resources = new ConcurrentDictionary<string, ConcurrentDictionary<string, string>>();
        _contributors = contributors ?? Enumerable.Empty<ILocalizationResourceContributor>();
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public string? GetString(string key, string cultureName)
    {
        EnsureInitialized();

        if (string.IsNullOrEmpty(key) || string.IsNullOrEmpty(cultureName))
        {
            return null;
        }

        // Try exact culture match first
        if (_resources.TryGetValue(cultureName, out var cultureResources) &&
            cultureResources.TryGetValue(key, out var value))
        {
            return value;
        }

        // Try parent cultures
        var culture = GetCultureInfo(cultureName);
        while (culture != null && !culture.Equals(CultureInfo.InvariantCulture))
        {
            culture = culture.Parent;
            if (culture != null &&
                _resources.TryGetValue(culture.Name, out var parentResources) &&
                parentResources.TryGetValue(key, out var parentValue))
            {
                return parentValue;
            }
        }

        return null;
    }

    /// <inheritdoc/>
    public string? GetString(string key)
    {
        return GetString(key, CultureInfo.CurrentUICulture.Name);
    }

    /// <inheritdoc/>
    public Dictionary<string, string> GetAllStrings(string cultureName, bool includeParentCultures = true)
    {
        EnsureInitialized();

        var result = new Dictionary<string, string>();

        if (string.IsNullOrEmpty(cultureName))
        {
            return result;
        }

        if (includeParentCultures)
        {
            // Start with parent cultures (lower priority)
            var culture = GetCultureInfo(cultureName);
            var cultures = new List<CultureInfo>();
            
            while (culture != null && !culture.Equals(CultureInfo.InvariantCulture))
            {
                cultures.Add(culture);
                culture = culture.Parent;
            }

            // Add strings from most general to most specific
            cultures.Reverse();
            foreach (var c in cultures)
            {
                if (_resources.TryGetValue(c.Name, out var cultureResources))
                {
                    foreach (var kvp in cultureResources)
                    {
                        result[kvp.Key] = kvp.Value;
                    }
                }
            }
        }
        else
        {
            // Only exact culture match
            if (_resources.TryGetValue(cultureName, out var exactResources))
            {
                foreach (var kvp in exactResources)
                {
                    result[kvp.Key] = kvp.Value;
                }
            }
        }

        return result;
    }

    /// <inheritdoc/>
    public IEnumerable<CultureInfo> GetSupportedCultures()
    {
        EnsureInitialized();

        return _resources.Keys
            .Select(GetCultureInfo)
            .Where(c => c != null)
            .Cast<CultureInfo>()
            .Distinct();
    }

    /// <inheritdoc/>
    public bool IsCultureSupported(string cultureName)
    {
        EnsureInitialized();

        if (string.IsNullOrEmpty(cultureName))
        {
            return false;
        }

        return _resources.ContainsKey(cultureName);
    }

    /// <inheritdoc/>
    public Task SetStringAsync(string key, string value, string cultureName)
    {
        if (string.IsNullOrEmpty(key) || string.IsNullOrEmpty(cultureName))
        {
            throw new ArgumentException("Key and culture name cannot be null or empty");
        }

        var cultureResources = _resources.GetOrAdd(cultureName, 
            _ => new ConcurrentDictionary<string, string>());
        
        cultureResources.AddOrUpdate(key, value, (_, _) => value);
        
        _logger.LogDebug("Set localization resource: Key='{Key}', Culture='{Culture}', Value='{Value}'", 
            key, cultureName, value);

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task RemoveStringAsync(string key, string cultureName)
    {
        if (string.IsNullOrEmpty(key) || string.IsNullOrEmpty(cultureName))
        {
            return Task.CompletedTask;
        }

        if (_resources.TryGetValue(cultureName, out var cultureResources))
        {
            cultureResources.TryRemove(key, out _);
            _logger.LogDebug("Removed localization resource: Key='{Key}', Culture='{Culture}'", key, cultureName);
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public async Task ReloadAsync()
    {
        lock (_lock)
        {
            _resources.Clear();
            _isInitialized = false;
        }

        await InitializeAsync();
        
        _logger.LogInformation("Localization resources reloaded");
    }

    /// <summary>
    /// Ensures the resource manager is initialized.
    /// </summary>
    private void EnsureInitialized()
    {
        if (!_isInitialized)
        {
            lock (_lock)
            {
                if (!_isInitialized)
                {
                    InitializeAsync().ConfigureAwait(false).GetAwaiter().GetResult();
                }
            }
        }
    }

    /// <summary>
    /// Initializes resources from contributors.
    /// </summary>
    private async Task InitializeAsync()
    {
        try
        {
            // Get all supported cultures from contributors
            var cultures = new HashSet<string>();
            
            foreach (var contributor in _contributors.OrderByDescending(c => c.Priority))
            {
                // For now, we'll load common cultures. In a real implementation, 
                // contributors would specify which cultures they support
                var supportedCultures = new[] { "en", "en-US", "zh", "zh-CN", "zh-TW", "ja", "ko", "fr", "de", "es" };
                
                foreach (var culture in supportedCultures)
                {
                    cultures.Add(culture);
                }
            }

            // Load resources for each culture
            foreach (var culture in cultures)
            {
                var context = new LocalizationResourceContributionContext(culture);
                
                foreach (var contributor in _contributors.OrderByDescending(c => c.Priority))
                {
                    try
                    {
                        await contributor.ContributeAsync(context);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error loading resources from contributor {ContributorType} for culture {Culture}", 
                            contributor.GetType().Name, culture);
                    }
                }

                // Add resources to the manager
                if (context.Resources.Any())
                {
                    var cultureResources = _resources.GetOrAdd(culture, 
                        _ => new ConcurrentDictionary<string, string>());
                    
                    foreach (var resource in context.Resources)
                    {
                        cultureResources.AddOrUpdate(resource.Key, resource.Value, (_, _) => resource.Value);
                    }
                    
                    _logger.LogDebug("Loaded {Count} resources for culture {Culture}", 
                        context.Resources.Count, culture);
                }
            }

            _isInitialized = true;
            _logger.LogInformation("Localization resource manager initialized with {CultureCount} cultures", 
                _resources.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initializing localization resource manager");
            throw;
        }
    }

    /// <summary>
    /// Gets culture info safely.
    /// </summary>
    private static CultureInfo? GetCultureInfo(string cultureName)
    {
        try
        {
            return CultureInfo.GetCultureInfo(cultureName);
        }
        catch (CultureNotFoundException)
        {
            return null;
        }
    }
}