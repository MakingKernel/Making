using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using System.Globalization;
using Making.Localization.Resources;

namespace Making.Localization.Localization;

/// <summary>
/// Making framework implementation of string localizer.
/// </summary>
public class MakingStringLocalizer : IMakingStringLocalizer
{
    private readonly IStringLocalizer _innerLocalizer;
    private readonly ILocalizationResourceManager _resourceManager;
    private readonly ILogger<MakingStringLocalizer> _logger;

    public MakingStringLocalizer(
        IStringLocalizer innerLocalizer,
        ILocalizationResourceManager resourceManager,
        ILogger<MakingStringLocalizer> logger)
    {
        _innerLocalizer = innerLocalizer ?? throw new ArgumentNullException(nameof(innerLocalizer));
        _resourceManager = resourceManager ?? throw new ArgumentNullException(nameof(resourceManager));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public LocalizedString this[string name] => GetString(name);

    /// <inheritdoc/>
    public LocalizedString this[string name, params object[] arguments] => GetString(name, arguments);

    /// <inheritdoc/>
    public LocalizedString GetString(string name, params object[] arguments)
    {
        return GetString(name, CultureInfo.CurrentUICulture.Name, arguments);
    }

    /// <inheritdoc/>
    public LocalizedString GetString(string name, string culture, params object[] arguments)
    {
        try
        {
            // First try to get from resource manager
            var localizedString = _resourceManager.GetString(name, culture);
            
            if (localizedString != null)
            {
                if (arguments.Length > 0)
                {
                    try
                    {
                        return new LocalizedString(name, string.Format(localizedString, arguments));
                    }
                    catch (FormatException ex)
                    {
                        _logger.LogWarning(ex, "Format error for localization key '{Key}' with culture '{Culture}'", name, culture);
                        return new LocalizedString(name, localizedString, true);
                    }
                }
                return new LocalizedString(name, localizedString);
            }

            // Fallback to inner localizer
            var innerResult = arguments.Length > 0 
                ? _innerLocalizer[name, arguments] 
                : _innerLocalizer[name];
            
            if (!innerResult.ResourceNotFound)
            {
                return innerResult;
            }

            // Log missing resource
            _logger.LogDebug("Localization key '{Key}' not found for culture '{Culture}'", name, culture);
            
            // Return key if not found
            return new LocalizedString(name, name, true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting localized string for key '{Key}' and culture '{Culture}'", name, culture);
            return new LocalizedString(name, name, true);
        }
    }

    /// <inheritdoc/>
    public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures)
    {
        return GetAllStrings(CultureInfo.CurrentUICulture.Name, includeParentCultures);
    }

    /// <inheritdoc/>
    public IEnumerable<LocalizedString> GetAllStrings(string culture, bool includeParentCultures)
    {
        try
        {
            var strings = new List<LocalizedString>();
            
            // Get from resource manager
            var resourceStrings = _resourceManager.GetAllStrings(culture, includeParentCultures);
            strings.AddRange(resourceStrings.Select(kvp => new LocalizedString(kvp.Key, kvp.Value)));
            
            // Get from inner localizer
            var innerStrings = _innerLocalizer.GetAllStrings(includeParentCultures);
            strings.AddRange(innerStrings.Where(s => !strings.Any(rs => rs.Name == s.Name)));
            
            return strings;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all localized strings for culture '{Culture}'", culture);
            return Enumerable.Empty<LocalizedString>();
        }
    }
}

/// <summary>
/// Generic implementation of Making string localizer.
/// </summary>
/// <typeparam name="TResource">The resource type.</typeparam>
public class MakingStringLocalizer<TResource> : IMakingStringLocalizer<TResource>
{
    private readonly IMakingStringLocalizer _localizer;

    public MakingStringLocalizer(IMakingStringLocalizer localizer)
    {
        _localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));
    }

    /// <inheritdoc/>
    public LocalizedString this[string name] => _localizer[name];

    /// <inheritdoc/>
    public LocalizedString this[string name, params object[] arguments] => _localizer[name, arguments];

    /// <inheritdoc/>
    public LocalizedString GetString(string name, params object[] arguments)
    {
        return _localizer.GetString(name, arguments);
    }

    /// <inheritdoc/>
    public LocalizedString GetString(string name, string culture, params object[] arguments)
    {
        return _localizer.GetString(name, culture, arguments);
    }

    /// <inheritdoc/>
    public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures)
    {
        return _localizer.GetAllStrings(includeParentCultures);
    }

    /// <inheritdoc/>
    public IEnumerable<LocalizedString> GetAllStrings(string culture, bool includeParentCultures = true)
    {
        return _localizer.GetAllStrings(culture, includeParentCultures);
    }
}