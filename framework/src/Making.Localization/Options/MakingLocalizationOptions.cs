namespace Making.Localization.Options;

/// <summary>
/// Configuration options for Making localization system.
/// </summary>
public class MakingLocalizationOptions
{
    /// <summary>
    /// Gets or sets the default culture name.
    /// </summary>
    public string DefaultCulture { get; set; } = "en";

    /// <summary>
    /// Gets or sets the list of supported cultures.
    /// </summary>
    public string[] SupportedCultures { get; set; } = { "en" };

    /// <summary>
    /// Gets or sets whether to fallback to the default culture when a localization is not found.
    /// </summary>
    public bool FallbackToDefaultCulture { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to fallback to parent cultures when a localization is not found.
    /// </summary>
    public bool FallbackToParentCultures { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to return the localization key when no localization is found.
    /// </summary>
    public bool ReturnKeyOnMissing { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to throw an exception when no localization is found.
    /// </summary>
    public bool ThrowOnMissing { get; set; } = false;

    /// <summary>
    /// Gets or sets the resource path for file-based localization providers.
    /// </summary>
    public string ResourcesPath { get; set; } = "Resources/Localization";

    /// <summary>
    /// Gets or sets whether to enable caching of localization resources.
    /// </summary>
    public bool EnableCaching { get; set; } = true;

    /// <summary>
    /// Gets or sets the cache expiration time in minutes.
    /// </summary>
    public int CacheExpirationMinutes { get; set; } = 60;

    /// <summary>
    /// Gets or sets whether to watch for file changes and reload resources automatically.
    /// </summary>
    public bool WatchForChanges { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to include parent culture resources by default.
    /// </summary>
    public bool IncludeParentCultures { get; set; } = true;

    /// <summary>
    /// Gets or sets the separator used for nested localization keys.
    /// </summary>
    public string KeySeparator { get; set; } = ".";

    /// <summary>
    /// Gets or sets custom resource contributor types.
    /// </summary>
    public Type[] ResourceContributorTypes { get; set; } = Array.Empty<Type>();

    /// <summary>
    /// Validates the options configuration.
    /// </summary>
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(DefaultCulture))
        {
            throw new InvalidOperationException("DefaultCulture cannot be null or empty");
        }

        if (SupportedCultures == null || SupportedCultures.Length == 0)
        {
            throw new InvalidOperationException("SupportedCultures cannot be null or empty");
        }

        if (!SupportedCultures.Contains(DefaultCulture))
        {
            throw new InvalidOperationException($"DefaultCulture '{DefaultCulture}' must be included in SupportedCultures");
        }

        if (CacheExpirationMinutes <= 0)
        {
            throw new InvalidOperationException("CacheExpirationMinutes must be greater than 0");
        }

        if (string.IsNullOrWhiteSpace(ResourcesPath))
        {
            throw new InvalidOperationException("ResourcesPath cannot be null or empty");
        }

        if (string.IsNullOrWhiteSpace(KeySeparator))
        {
            throw new InvalidOperationException("KeySeparator cannot be null or empty");
        }
    }
}

/// <summary>
/// Culture configuration for localization.
/// </summary>
public class CultureConfiguration
{
    /// <summary>
    /// Gets or sets the culture name (e.g., "en-US").
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Gets or sets the display name of the culture.
    /// </summary>
    public string? DisplayName { get; set; }

    /// <summary>
    /// Gets or sets whether this culture is enabled.
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the priority of this culture (higher values = higher priority).
    /// </summary>
    public int Priority { get; set; } = 0;

    /// <summary>
    /// Gets or sets whether this culture is right-to-left.
    /// </summary>
    public bool IsRightToLeft { get; set; }

    /// <summary>
    /// Gets or sets additional metadata for this culture.
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();

    /// <summary>
    /// Gets the culture info object.
    /// </summary>
    public System.Globalization.CultureInfo GetCultureInfo()
    {
        return System.Globalization.CultureInfo.GetCultureInfo(Name);
    }
}

/// <summary>
/// Localization resource configuration.
/// </summary>
public class LocalizationResourceConfiguration
{
    /// <summary>
    /// Gets or sets the resource type.
    /// </summary>
    public Type? ResourceType { get; set; }

    /// <summary>
    /// Gets or sets the base name for the resource.
    /// </summary>
    public string? BaseName { get; set; }

    /// <summary>
    /// Gets or sets the assembly containing the resources.
    /// </summary>
    public System.Reflection.Assembly? Assembly { get; set; }

    /// <summary>
    /// Gets or sets the priority of this resource configuration.
    /// </summary>
    public int Priority { get; set; } = 0;

    /// <summary>
    /// Gets or sets whether this resource configuration is enabled.
    /// </summary>
    public bool IsEnabled { get; set; } = true;
}