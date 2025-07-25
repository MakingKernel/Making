# Making.Analyzers

Dependency injection source generator for the Making framework.

## Overview

Making.Analyzers provides Roslyn-based source generators and code analyzers for the Making framework. It automatically generates dependency injection registration code, validates service configurations, and provides compile-time analysis for better development experience.

## Features

- **Dependency Injection Generator**: Automatically generates service registration code
- **Service Validation**: Compile-time validation of service configurations
- **Diagnostic Descriptors**: Rich diagnostic messages and error reporting
- **Source Generation**: Reduces boilerplate code with automatic code generation
- **Roslyn Integration**: Leverages Microsoft.CodeAnalysis for powerful analysis

## Installation

```bash
dotnet add package Making.Analyzers
```

## Usage

### Service Registration Generation

```csharp
// Making your services with lifetime attributes
[Singleton]
public class EmailService : IEmailService
{
    public async Task SendEmailAsync(string to, string subject, string body)
    {
        // Implementation
    }
}

[Scoped]
public class UserService : IUserService
{
    private readonly IEmailService _emailService;
    
    public UserService(IEmailService emailService)
    {
        _emailService = emailService;
    }
    
    public async Task CreateUserAsync(CreateUserRequest request)
    {
        // Implementation
        await _emailService.SendEmailAsync(request.Email, "Welcome", "Welcome to our platform!");
    }
}

[Transient]
public class OrderProcessor : IOrderProcessor
{
    public async Task ProcessOrderAsync(Order order)
    {
        // Implementation
    }
}
```

### Generated Registration Code

The analyzer automatically generates extension methods for service registration:

```csharp
// Generated code (you don't need to write this)
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddGeneratedServices(this IServiceCollection services)
    {
        services.AddSingleton<IEmailService, EmailService>();
        services.AddScoped<IUserService, UserService>();
        services.AddTransient<IOrderProcessor, OrderProcessor>();
        
        return services;
    }
}
```

### Using Generated Registration

```csharp
public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        // Use the generated registration method
        services.AddGeneratedServices();
        
        // Add other services manually if needed
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(connectionString));
    }
}
```

### Service Validation

The analyzer validates service configurations and provides warnings:

```csharp
// This will generate a diagnostic warning
[Singleton]
public class BadService : IBadService
{
    // Warning: Singleton service depends on Scoped service
    public BadService(IScopedService scopedService) // ⚠️ Warning
    {
    }
}

// This will generate a diagnostic error
[Scoped]
public class InvalidService // ❌ Error: No interface implemented
{
    // Error: Service must implement an interface
}
```

### Diagnostic Messages

The analyzer provides helpful diagnostic messages:

- **MARK001**: Service must implement at least one interface
- **MARK002**: Singleton service should not depend on Scoped services
- **MARK003**: Singleton service should not depend on Transient services
- **MARK004**: Service interface not found in current assembly
- **MARK005**: Circular dependency detected

### Advanced Usage

```csharp
// Multiple interfaces
[Scoped]
public class MultiService : IService1, IService2, IService3
{
    // Will register for all implemented interfaces
}

// Generic services
[Scoped]
public class Repository<T> : IRepository<T> where T : class
{
    // Generic service registration
}

// Named services
[Scoped("PrimaryDatabase")]
public class PrimaryDbService : IDbService
{
    // Named service registration
}

[Scoped("SecondaryDatabase")]
public class SecondaryDbService : IDbService
{
    // Another named service registration
}
```

### Configuration Options

Create a `mark.analyzers.json` file in your project root:

```json
{
  "generateRegistrationMethods": true,
  "validateServiceLifetimes": true,
  "validateCircularDependencies": true,
  "generateNamesapce": "MyApp.Generated",
  "generateClassName": "MakingServiceRegistration",
  "excludeAssemblies": ["System.*", "Microsoft.*"],
  "includeInternalServices": false
}
```

### MSBuild Integration

The analyzer integrates seamlessly with MSBuild:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <!-- Enable source generation debugging -->
    <EmitCompilerGeneratedFiles>true</EmitCompilerGeneratedFiles>
    <CompilerGeneratedFilesOutputPath>Generated</CompilerGeneratedFilesOutputPath>
  </PropertyGroup>
  
  <ItemGroup>
    <PackageReference Include="Making.Analyzers" Version="1.0.0">
      <PrivateAssets>all</PrivateAssets>
      <IncludeAssets>runtime; build; native; contentfiles; analyzers</IncludeAssets>
    </PackageReference>
  </ItemGroup>
  
</Project>
```

### Custom Attributes

Define custom service lifetime attributes:

```csharp
[AttributeUsage(AttributeTargets.Class)]
public class RepositoryAttribute : Attribute
{
    public ServiceLifetime Lifetime { get; }
    
    public RepositoryAttribute(ServiceLifetime lifetime = ServiceLifetime.Scoped)
    {
        Lifetime = lifetime;
    }
}

// Usage
[Repository(ServiceLifetime.Singleton)]
public class CacheRepository : IRepository
{
    // Implementation
}
```

## Requirements

- .NET Standard 2.0+
- Microsoft.CodeAnalysis.CSharp (for compilation)
- C# 9.0+ (for source generators)

## License

This project is part of the Making framework.