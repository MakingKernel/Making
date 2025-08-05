using System.Globalization;

namespace Making.Localization.Resources;

/// <summary>
/// Interface for managing localization resources.
/// </summary>
public interface ILocalizationResourceManager
{
    /// <summary>
    /// Gets a localized string for the specified key and culture.
    /// </summary>
    /// <param name="key">The localization key.</param>
    /// <param name="cultureName">The culture name.</param>
    /// <returns>The localized string or null if not found.</returns>
    string? GetString(string key, string cultureName);

    /// <summary>
    /// Gets a localized string for the specified key and current culture.
    /// </summary>
    /// <param name="key">The localization key.</param>
    /// <returns>The localized string or null if not found.</returns>
    string? GetString(string key);

    /// <summary>
    /// Gets all localized strings for the specified culture.
    /// </summary>
    /// <param name="cultureName">The culture name.</param>
    /// <param name="includeParentCultures">Whether to include parent culture strings.</param>
    /// <returns>Dictionary of localization key-value pairs.</returns>
    Dictionary<string, string> GetAllStrings(string cultureName, bool includeParentCultures = true);

    /// <summary>
    /// Gets all supported cultures.
    /// </summary>
    /// <returns>List of supported culture info.</returns>
    IEnumerable<CultureInfo> GetSupportedCultures();

    /// <summary>
    /// Checks if a culture is supported.
    /// </summary>
    /// <param name="cultureName">The culture name to check.</param>
    /// <returns>True if supported, false otherwise.</returns>
    bool IsCultureSupported(string cultureName);

    /// <summary>
    /// Adds or updates a localization resource.
    /// </summary>
    /// <param name="key">The localization key.</param>
    /// <param name="value">The localized value.</param>
    /// <param name="cultureName">The culture name.</param>
    Task SetStringAsync(string key, string value, string cultureName);

    /// <summary>
    /// Removes a localization resource.
    /// </summary>
    /// <param name="key">The localization key.</param>
    /// <param name="cultureName">The culture name.</param>
    Task RemoveStringAsync(string key, string cultureName);

    /// <summary>
    /// Reloads localization resources from the underlying store.
    /// </summary>
    Task ReloadAsync();
}

/// <summary>
/// Resource information model.
/// </summary>
public class LocalizationResource
{
    public required string Key { get; set; }
    public required string Value { get; set; }
    public required string CultureName { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}

/// <summary>
/// Interface for localization resource contributors.
/// </summary>
public interface ILocalizationResourceContributor
{
    /// <summary>
    /// Gets the priority of this contributor (higher values = higher priority).
    /// </summary>
    int Priority { get; }

    /// <summary>
    /// Contributes localization resources.
    /// </summary>
    /// <param name="context">The contribution context.</param>
    Task ContributeAsync(LocalizationResourceContributionContext context);
}

/// <summary>
/// Context for localization resource contribution.
/// </summary>
public class LocalizationResourceContributionContext
{
    public string CultureName { get; }
    public Dictionary<string, string> Resources { get; }

    public LocalizationResourceContributionContext(string cultureName)
    {
        CultureName = cultureName;
        Resources = new Dictionary<string, string>();
    }

    /// <summary>
    /// Adds a localization resource.
    /// </summary>
    /// <param name="key">The localization key.</param>
    /// <param name="value">The localized value.</param>
    public void AddResource(string key, string value)
    {
        Resources[key] = value;
    }
}