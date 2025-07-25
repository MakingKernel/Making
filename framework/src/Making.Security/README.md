# Making.Security

Security utilities and extensions for the Making framework.

## Overview

Making.Security provides essential security components and utilities for the Making framework. It includes current user management, claims handling, principal access, and security-related extensions for building secure applications.

## Features

- **Current User Access**: Easy access to current user information
- **Claims Management**: Comprehensive claims handling and extraction
- **Principal Access**: Current principal accessor for security context
- **Security Extensions**: ClaimsIdentity extensions for common operations
- **User Context**: Centralized user context management
- **Security Abstractions**: Core security interfaces and contracts

## Installation

```bash
dotnet add package Making.Security
```

## Usage

### Register Services

```csharp
services.AddMarkSecurity();
```

### Current User Access

```csharp
public class UserService
{
    private readonly ICurrentUser _currentUser;
    
    public UserService(ICurrentUser currentUser)
    {
        _currentUser = currentUser;
    }
    
    public async Task<UserProfile> GetCurrentUserProfileAsync()
    {
        var userId = _currentUser.UserId;
        var userName = _currentUser.UserName;
        var email = _currentUser.Email;
        var roles = _currentUser.Roles;
        
        return new UserProfile
        {
            Id = userId,
            Name = userName,
            Email = email,
            Roles = roles
        };
    }
    
    public bool CanAccessResource(string resourceId)
    {
        return _currentUser.IsInRole("Admin") || 
               _currentUser.HasClaim("Resource", resourceId);
    }
}
```

### Claims Extensions

```csharp
public class AuthController : ControllerBase
{
    [HttpGet("profile")]
    public IActionResult GetProfile()
    {
        var identity = HttpContext.User.Identity as ClaimsIdentity;
        
        var userId = identity.GetUserId();
        var userName = identity.GetUserName();
        var email = identity.GetEmail();
        var roles = identity.GetRoles();
        
        return Ok(new
        {
            UserId = userId,
            UserName = userName,
            Email = email,
            Roles = roles
        });
    }
}
```

### Custom Claims

```csharp
public class CustomAuthService
{
    private readonly ICurrentPrincipalAccessor _principalAccessor;
    
    public CustomAuthService(ICurrentPrincipalAccessor principalAccessor)
    {
        _principalAccessor = principalAccessor;
    }
    
    public bool HasPermission(string permission)
    {
        var principal = _principalAccessor.Principal;
        return principal?.HasClaim(MarkClaimType.Permission, permission) ?? false;
    }
    
    public string GetTenantId()
    {
        var principal = _principalAccessor.Principal;
        return principal?.FindFirst(MarkClaimType.TenantId)?.Value;
    }
}
```

### Making Claim Types

```csharp
public static class MarkClaimType
{
    public const string UserId = "mark:userid";
    public const string UserName = "mark:username";
    public const string Email = "mark:email";
    public const string Role = "mark:role";
    public const string Permission = "mark:permission";
    public const string TenantId = "mark:tenantid";
}
```

### Authorization Policies

```csharp
public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddMarkSecurity();
        
        services.AddAuthorization(options =>
        {
            options.AddPolicy("RequireAdminRole", policy =>
                policy.RequireClaim(MarkClaimType.Role, "Admin"));
                
            options.AddPolicy("RequireUserPermission", policy =>
                policy.RequireClaim(MarkClaimType.Permission, "user:read"));
        });
    }
}

[Authorize(Policy = "RequireAdminRole")]
public class AdminController : ControllerBase
{
    // Admin-only actions
}
```

### Multi-Tenant Security

```csharp
public class TenantService
{
    private readonly ICurrentUser _currentUser;
    
    public TenantService(ICurrentUser currentUser)
    {
        _currentUser = currentUser;
    }
    
    public async Task<List<Order>> GetOrdersAsync()
    {
        var tenantId = _currentUser.TenantId;
        
        // Filter orders by current user's tenant
        return await _orderRepository.GetByTenantAsync(tenantId);
    }
}
```

## Requirements

- .NET Standard 2.0+
- ASP.NET Core
- Microsoft.Extensions.DependencyInjection.Abstractions
- Making.Core

## License

This project is part of the Making framework.