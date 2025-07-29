using Making.Localization.Resources;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Making.Localization.Contributors;

/// <summary>
/// JSON file-based localization resource contributor.
/// </summary>
public class JsonFileLocalizationResourceContributor : ILocalizationResourceContributor
{
    private readonly string _resourcesPath;
    private readonly ILogger<JsonFileLocalizationResourceContributor> _logger;

    public JsonFileLocalizationResourceContributor(
        string resourcesPath,
        ILogger<JsonFileLocalizationResourceContributor> logger)
    {
        _resourcesPath = resourcesPath ?? throw new ArgumentNullException(nameof(resourcesPath));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public int Priority => 100; // Default priority

    /// <inheritdoc/>
    public async Task ContributeAsync(LocalizationResourceContributionContext context)
    {
        try
        {
            var filePath = Path.Combine(_resourcesPath, $"{context.CultureName}.json");
            
            if (!File.Exists(filePath))
            {
                // Try with parent culture if specific culture file doesn't exist
                var parentCulture = GetParentCulture(context.CultureName);
                if (!string.IsNullOrEmpty(parentCulture))
                {
                    filePath = Path.Combine(_resourcesPath, $"{parentCulture}.json");
                }
                
                if (!File.Exists(filePath))
                {
                    _logger.LogDebug("Localization file not found: {FilePath}", filePath);
                    return;
                }
            }

            var jsonContent = await File.ReadAllTextAsync(filePath);
            if (string.IsNullOrWhiteSpace(jsonContent))
            {
                _logger.LogWarning("Localization file is empty: {FilePath}", filePath);
                return;
            }

            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                ReadCommentHandling = JsonCommentHandling.Skip,
                AllowTrailingCommas = true
            };

            var resources = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonContent, options);
            if (resources == null)
            {
                _logger.LogWarning("Failed to deserialize localization file: {FilePath}", filePath);
                return;
            }

            // Flatten nested JSON structure
            FlattenResources(resources, context, string.Empty);
            
            _logger.LogDebug("Loaded {Count} resources from {FilePath}", 
                context.Resources.Count, filePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading localization resources for culture {Culture} from path {Path}", 
                context.CultureName, _resourcesPath);
        }
    }

    /// <summary>
    /// Flattens nested JSON structure into flat key-value pairs.
    /// </summary>
    private void FlattenResources(Dictionary<string, object> resources, 
        LocalizationResourceContributionContext context, string prefix)
    {
        foreach (var kvp in resources)
        {
            var key = string.IsNullOrEmpty(prefix) ? kvp.Key : $"{prefix}.{kvp.Key}";
            
            if (kvp.Value is JsonElement jsonElement)
            {
                switch (jsonElement.ValueKind)
                {
                    case JsonValueKind.String:
                        context.AddResource(key, jsonElement.GetString() ?? string.Empty);
                        break;
                    
                    case JsonValueKind.Object:
                        var nestedDict = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonElement.GetRawText());
                        if (nestedDict != null)
                        {
                            FlattenResources(nestedDict, context, key);
                        }
                        break;
                    
                    case JsonValueKind.Number:
                        context.AddResource(key, jsonElement.ToString());
                        break;
                    
                    case JsonValueKind.True:
                    case JsonValueKind.False:
                        context.AddResource(key, jsonElement.GetBoolean().ToString());
                        break;
                    
                    default:
                        context.AddResource(key, jsonElement.ToString());
                        break;
                }
            }
            else if (kvp.Value is string stringValue)
            {
                context.AddResource(key, stringValue);
            }
            else if (kvp.Value != null)
            {
                context.AddResource(key, kvp.Value.ToString() ?? string.Empty);
            }
        }
    }

    /// <summary>
    /// Gets the parent culture name.
    /// </summary>
    private static string? GetParentCulture(string cultureName)
    {
        if (string.IsNullOrEmpty(cultureName) || !cultureName.Contains('-'))
        {
            return null;
        }

        var dashIndex = cultureName.LastIndexOf('-');
        return dashIndex > 0 ? cultureName[..dashIndex] : null;
    }
}

/// <summary>
/// Embedded resource-based localization resource contributor.
/// </summary>
public class EmbeddedResourceLocalizationResourceContributor : ILocalizationResourceContributor
{
    private readonly Type _resourceType;
    private readonly ILogger<EmbeddedResourceLocalizationResourceContributor> _logger;

    public EmbeddedResourceLocalizationResourceContributor(
        Type resourceType,
        ILogger<EmbeddedResourceLocalizationResourceContributor> logger)
    {
        _resourceType = resourceType ?? throw new ArgumentNullException(nameof(resourceType));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public int Priority => 50; // Lower priority than file-based

    /// <inheritdoc/>
    public async Task ContributeAsync(LocalizationResourceContributionContext context)
    {
        try
        {
            var assembly = _resourceType.Assembly;
            var resourceName = $"{_resourceType.Namespace}.{context.CultureName}.json";
            
            using var stream = assembly.GetManifestResourceStream(resourceName);
            if (stream == null)
            {
                // Try with parent culture
                var parentCulture = GetParentCulture(context.CultureName);
                if (!string.IsNullOrEmpty(parentCulture))
                {
                    resourceName = $"{_resourceType.Namespace}.{parentCulture}.json";
                    using var parentStream = assembly.GetManifestResourceStream(resourceName);
                    if (parentStream != null)
                    {
                        await LoadFromStreamAsync(parentStream, context);
                        return;
                    }
                }
                
                _logger.LogDebug("Embedded localization resource not found: {ResourceName}", resourceName);
                return;
            }

            await LoadFromStreamAsync(stream, context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading embedded localization resources for culture {Culture}", 
                context.CultureName);
        }
    }

    /// <summary>
    /// Loads resources from stream.
    /// </summary>
    private async Task LoadFromStreamAsync(Stream stream, LocalizationResourceContributionContext context)
    {
        using var reader = new StreamReader(stream);
        var jsonContent = await reader.ReadToEndAsync();
        
        if (string.IsNullOrWhiteSpace(jsonContent))
        {
            return;
        }

        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true
        };

        var resources = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonContent, options);
        if (resources == null)
        {
            return;
        }

        FlattenResources(resources, context, string.Empty);
        
        _logger.LogDebug("Loaded {Count} embedded resources for culture {Culture}", 
            context.Resources.Count, context.CultureName);
    }

    /// <summary>
    /// Flattens nested JSON structure into flat key-value pairs.
    /// </summary>
    private void FlattenResources(Dictionary<string, object> resources, 
        LocalizationResourceContributionContext context, string prefix)
    {
        foreach (var kvp in resources)
        {
            var key = string.IsNullOrEmpty(prefix) ? kvp.Key : $"{prefix}.{kvp.Key}";
            
            if (kvp.Value is JsonElement jsonElement)
            {
                switch (jsonElement.ValueKind)
                {
                    case JsonValueKind.String:
                        context.AddResource(key, jsonElement.GetString() ?? string.Empty);
                        break;
                    
                    case JsonValueKind.Object:
                        var nestedDict = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonElement.GetRawText());
                        if (nestedDict != null)
                        {
                            FlattenResources(nestedDict, context, key);
                        }
                        break;
                    
                    default:
                        context.AddResource(key, jsonElement.ToString());
                        break;
                }
            }
            else if (kvp.Value is string stringValue)
            {
                context.AddResource(key, stringValue);
            }
            else if (kvp.Value != null)
            {
                context.AddResource(key, kvp.Value.ToString() ?? string.Empty);
            }
        }
    }

    /// <summary>
    /// Gets the parent culture name.
    /// </summary>
    private static string? GetParentCulture(string cultureName)
    {
        if (string.IsNullOrEmpty(cultureName) || !cultureName.Contains('-'))
        {
            return null;
        }

        var dashIndex = cultureName.LastIndexOf('-');
        return dashIndex > 0 ? cultureName[..dashIndex] : null;
    }
}