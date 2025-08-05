using Microsoft.Extensions.Localization;

namespace Making.Localization.Localization;

/// <summary>
/// Enhanced string localizer interface with additional Making framework features.
/// </summary>
public interface IMakingStringLocalizer : IStringLocalizer
{
    /// <summary>
    /// Gets a localized string with the specified name and arguments.
    /// Returns the key if localization is not found and fallback is disabled.
    /// </summary>
    /// <param name="name">The key name.</param>
    /// <param name="arguments">The arguments for formatting.</param>
    /// <returns>The localized string.</returns>
    LocalizedString GetString(string name, params object[] arguments);

    /// <summary>
    /// Gets a localized string with the specified name and arguments for a specific culture.
    /// </summary>
    /// <param name="name">The key name.</param>
    /// <param name="culture">The culture to use for localization.</param>
    /// <param name="arguments">The arguments for formatting.</param>
    /// <returns>The localized string.</returns>
    LocalizedString GetString(string name, string culture, params object[] arguments);

    /// <summary>
    /// Gets all localized strings for the current culture.
    /// </summary>
    /// <param name="includeParentCultures">Whether to include strings from parent cultures.</param>
    /// <returns>All localized strings.</returns>
    IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures = true);

    /// <summary>
    /// Gets all localized strings for a specific culture.
    /// </summary>
    /// <param name="culture">The culture to get strings for.</param>
    /// <param name="includeParentCultures">Whether to include strings from parent cultures.</param>
    /// <returns>All localized strings.</returns>
    IEnumerable<LocalizedString> GetAllStrings(string culture, bool includeParentCultures = true);
}

/// <summary>
/// Generic string localizer interface for typed resources.
/// </summary>
/// <typeparam name="TResource">The resource type.</typeparam>
public interface IMakingStringLocalizer<TResource> : IMakingStringLocalizer, IStringLocalizer<TResource>
{
}