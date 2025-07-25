# Making.AspNetCore

ASP.NET Core integration and extensions for the Making framework.

## Overview

Making.AspNetCore provides essential components for building web applications with ASP.NET Core using the Making framework. It includes MiniAPI attributes, pagination models, and standardized result DTOs.

## Features

- **MiniAPI Attributes**: Attributes for defining lightweight API endpoints
- **Pagination Support**: Built-in paging models and result wrappers
- **Standardized Results**: Consistent DTO patterns for API responses
- **ASP.NET Core Integration**: Seamless integration with ASP.NET Core pipeline

## Installation

```bash
dotnet add package Making.AspNetCore
```

## Usage

### MiniAPI Attributes

```csharp
[MiniApi]
public class UserController
{
    [HttpGet("/users")]
    public async Task<PagedResult<User>> GetUsers([FromQuery] PagingModel paging)
    {
        // Implementation
    }
}
```

### Pagination

```csharp
public class UserService
{
    public async Task<PagedResult<User>> GetUsersAsync(PagingModel paging)
    {
        var users = await GetUsersFromDatabase(paging.PageNumber, paging.PageSize);
        var totalCount = await GetTotalUsersCount();
        
        return new PagedResult<User>
        {
            Items = users,
            TotalCount = totalCount,
            PageNumber = paging.PageNumber,
            PageSize = paging.PageSize
        };
    }
}
```

### Result DTOs

```csharp
public class ApiController : ControllerBase
{
    [HttpGet("/api/data")]
    public async Task<ResultDto<string>> GetData()
    {
        return ResultDto<string>.Success("Hello World");
    }
}
```

## Requirements

- .NET 9.0+
- ASP.NET Core

## License

This project is part of the Making framework.