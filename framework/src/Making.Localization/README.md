# Making.Localization

Enterprise-grade localization and internationalization support for the Making framework.

## Key Features

- **Multi-Source Localization**: JSON files, embedded resources, and custom contributors
- **Culture Fallback**: Automatic fallback to parent cultures and default culture
- **Resource Management**: Pluggable resource management with caching support
- **Performance Optimized**: In-memory caching with configurable expiration
- **Multi-Tenant Support**: Culture-specific localization per tenant
- **Hot Reload**: File watching and automatic resource reloading
- **Extensible Architecture**: Custom resource contributors and providers

## Architecture

### Core Components

#### IMakingStringLocalizer
Enhanced localizer interface with:
- Culture-specific localization
- Fallback mechanisms
- Nested key support
- Format string handling

#### ILocalizationResourceManager
Resource management with:
- Multi-source resource loading
- Culture hierarchy support
- Caching mechanisms
- Dynamic resource updates

#### Resource Contributors
- **JsonFileLocalizationResourceContributor**: File-based localization
- **EmbeddedResourceLocalizationResourceContributor**: Assembly embedded resources
- **Custom Contributors**: Extensible contributor pattern

## Usage Examples

### Basic Setup
```csharp
services.AddMakingLocalizationWithDefaults("Resources/Localization");
```

### Advanced Configuration
```csharp
services.AddMakingLocalization(options =>
{
    options.DefaultCulture = "en";
    options.SupportedCultures = new[] { "en", "zh-CN", "ja", "ko" };
    options.FallbackToDefaultCulture = true;
    options.EnableCaching = true;
    options.WatchForChanges = true;
});

services.AddJsonFileLocalizationContributor("Resources/Localization");
services.AddEmbeddedLocalizationContributor<MyResourceClass>();
```

### Resource Files Structure
```
Resources/Localization/
├── en.json
├── zh-CN.json
├── ja.json
└── ko.json
```

### JSON Resource Format
```json
{
  "welcome": "Welcome to Making Framework",
  "user": {
    "profile": "User Profile",
    "settings": "User Settings"
  },
  "validation": {
    "required": "The {0} field is required",
    "email": "Please enter a valid email address"
  }
}
```

### Usage in Code
```csharp
public class UserController : ControllerBase
{
    private readonly IMakingStringLocalizer _localizer;

    public UserController(IMakingStringLocalizer localizer)
    {
        _localizer = localizer;
    }

    public IActionResult GetWelcome()
    {
        var message = _localizer["welcome"];
        var userProfile = _localizer["user.profile"];
        var validation = _localizer["validation.required", "Email"];
        
        return Ok(new { message, userProfile, validation });
    }
}
```

## Professional Features

1. **Enterprise Ready**: Comprehensive logging, error handling, and monitoring
2. **Performance Optimized**: Smart caching and efficient resource loading
3. **Scalable Architecture**: Plugin-based design for custom scenarios
4. **Culture Management**: Advanced culture hierarchy and fallback support
5. **Developer Experience**: IntelliSense support and compile-time checking
6. **Production Ready**: Configuration validation and health checks