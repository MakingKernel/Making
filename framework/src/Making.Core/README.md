# Making.Core

Core functionality and infrastructure for the Making framework.

## Overview

Making.Core provides the foundational components and utilities that power the Making framework. It includes essential extension methods, infrastructure classes, and common abstractions used across all Making framework packages.

## Features

- **Extension Methods**: Comprehensive extension methods for common types (String, DateTime, Stream, Assembly, etc.)
- **Service Lifetime Attributes**: Attributes for dependency injection lifecycle management
- **Soft Delete Support**: Interface for implementing soft delete patterns
- **Async Disposal**: Utilities for async resource cleanup
- **Parameter Validation**: Check utilities for argument validation
- **Platform Detection**: Cross-platform utilities for OS detection

## Installation

```bash
dotnet add package Making.Core
```

## Usage

### Basic Extensions

```csharp
using Making;

// String extensions
string value = "hello world".ToPascalCase(); // "HelloWorld"
bool isEmpty = "".IsNullOrEmpty(); // true

// DateTime extensions
DateTime now = DateTime.Now;
bool isWeekend = now.IsWeekend();

// Stream extensions
using var stream = new MemoryStream();
byte[] data = await stream.ReadAllBytesAsync();
```

### Service Lifetime Attributes

```csharp
[Singleton]
public class MyService : IMyService
{
    // Implementation
}

[Scoped]
public class ScopedService : IScopedService
{
    // Implementation
}

[Transient]
public class TransientService : ITransientService
{
    // Implementation
}
```

### Soft Delete

```csharp
public class User : ISoftDelete
{
    public bool IsDeleted { get; set; }
    public DateTime? DeletionTime { get; set; }
}
```

## Requirements

- .NET Standard 2.0+
- Microsoft.Extensions.DependencyInjection

## License

This project is part of the Making framework.