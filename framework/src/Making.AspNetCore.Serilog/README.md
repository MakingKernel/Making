# Making.AspNetCore.Serilog

Serilog integration for ASP.NET Core in the Making framework.

## Overview

Making.AspNetCore.Serilog provides seamless integration between Serilog structured logging and ASP.NET Core applications built with the Making framework. It includes middleware for request logging, multi-tenancy support, and security context enrichment.

## Features

- **Serilog Middleware**: Automatic request/response logging
- **Multi-Tenancy Support**: Tenant-aware logging with context enrichment
- **Security Integration**: User context logging and security event tracking
- **Performance Monitoring**: Request timing and performance metrics
- **Structured Logging**: Rich structured log events with proper correlation

## Installation

```bash
dotnet add package Making.AspNetCore.Serilog
```

## Usage

### Basic Setup

```csharp
using Making.AspNetCore.Serilog;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
builder.Host.UseSerilog((context, configuration) =>
    configuration.ReadFrom.Configuration(context.Configuration));

var app = builder.Build();

// Add Serilog middleware
app.UseMakingSerilogMiddleware();

app.Run();
```

### With Multi-Tenancy

```csharp
app.UseMakingSerilogMiddleware(options =>
{
    options.EnrichWithTenantInfo = true;
    options.EnrichWithUserInfo = true;
    options.LogRequestBody = true;
    options.LogResponseBody = false;
});
```

### Configuration Example

```json
{
  "Serilog": {
    "Using": ["Serilog.Sinks.Console", "Serilog.Sinks.File"],
    "MinimumLevel": "Information",
    "WriteTo": [
      { "Name": "Console" },
      {
        "Name": "File",
        "Args": {
          "path": "logs/mark-.txt",
          "rollingInterval": "Day"
        }
      }
    ],
    "Enrich": ["FromLogContext", "WithMachineName", "WithThreadId"],
    "Properties": {
      "Application": "Making.WebApp"
    }
  }
}
```

## Requirements

- .NET Standard 2.0+
- ASP.NET Core
- Serilog
- Making.Core
- Making.MultiTenancy.Abstractions
- Making.Security

## License

This project is part of the Making framework.