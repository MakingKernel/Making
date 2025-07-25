# Making.Jwt

JWT authentication and authorization support for the Making framework.

## Overview

Making.Jwt provides comprehensive JWT (JSON Web Token) authentication and authorization functionality for the Making framework. It includes token generation, validation, refresh token management, and ASP.NET Core integration.

## Features

- **JWT Token Generation**: Create secure JWT tokens with custom claims
- **Token Validation**: Comprehensive token validation with security checks
- **Refresh Token Support**: Secure refresh token implementation with storage abstraction
- **Claims Management**: Easy claims handling and extraction
- **ASP.NET Core Integration**: Middleware and authentication handlers
- **Configurable Options**: Flexible JWT configuration options

## Installation

```bash
dotnet add package Making.Jwt
```

## Usage

### Configuration

```json
{
  "Jwt": {
    "Issuer": "https://your-app.com",
    "Audience": "https://your-app.com",
    "SecretKey": "your-super-secret-key-here-must-be-at-least-32-characters",
    "ExpirationMinutes": 60,
    "RefreshTokenExpirationDays": 7,
    "ValidateIssuer": true,
    "ValidateAudience": true,
    "ValidateLifetime": true,
    "ValidateIssuerSigningKey": true
  }
}
```

### Register Services

```csharp
services.AddMakingJwt(configuration);
```

### Generate JWT Tokens

```csharp
public class AuthService
{
    private readonly IJwtService _jwtService;
    
    public AuthService(IJwtService jwtService)
    {
        _jwtService = jwtService;
    }
    
    public async Task<TokenResult> LoginAsync(string username, string password)
    {
        // Validate user credentials...
        
        var claims = new JwtClaims
        {
            UserId = user.Id,
            UserName = user.UserName,
            Email = user.Email,
            Roles = user.Roles.ToArray()
        };
        
        return await _jwtService.GenerateTokenAsync(claims);
    }
}
```

### Validate Tokens

```csharp
public class TokenController : ControllerBase
{
    private readonly IJwtService _jwtService;
    
    public TokenController(IJwtService jwtService)
    {
        _jwtService = jwtService;
    }
    
    [HttpPost("validate")]
    public async Task<IActionResult> ValidateToken([FromBody] string token)
    {
        var result = await _jwtService.ValidateTokenAsync(token);
        
        if (result.IsValid)
        {
            return Ok(new { Valid = true, Claims = result.Claims });
        }
        
        return BadRequest(new { Valid = false, Error = result.Error });
    }
}
```

### Refresh Tokens

```csharp
[HttpPost("refresh")]
public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request)
{
    var result = await _jwtService.RefreshTokenAsync(request.RefreshToken);
    
    if (result.IsValid)
    {
        return Ok(result.TokenResult);
    }
    
    return BadRequest(new { Error = result.Error });
}
```

### Custom Refresh Token Store

```csharp
public class DatabaseRefreshTokenStore : IRefreshTokenStore
{
    private readonly IDbContext _context;
    
    public DatabaseRefreshTokenStore(IDbContext context)
    {
        _context = context;
    }
    
    public async Task StoreAsync(string refreshToken, string userId, DateTime expirationTime)
    {
        var tokenEntity = new RefreshToken
        {
            Token = refreshToken,
            UserId = userId,
            ExpirationTime = expirationTime,
            CreatedAt = DateTime.UtcNow
        };
        
        _context.RefreshTokens.Add(tokenEntity);
        await _context.SaveChangesAsync();
    }
    
    public async Task<bool> ValidateAsync(string refreshToken)
    {
        var token = await _context.RefreshTokens
            .FirstOrDefaultAsync(t => t.Token == refreshToken);
        
        return token != null && token.ExpirationTime > DateTime.UtcNow;
    }
    
    public async Task RevokeAsync(string refreshToken)
    {
        var token = await _context.RefreshTokens
            .FirstOrDefaultAsync(t => t.Token == refreshToken);
        
        if (token != null)
        {
            _context.RefreshTokens.Remove(token);
            await _context.SaveChangesAsync();
        }
    }
}

// Register custom store
services.AddScoped<IRefreshTokenStore, DatabaseRefreshTokenStore>();
```

## Requirements

- .NET Standard 2.0+
- System.IdentityModel.Tokens.Jwt
- Microsoft.AspNetCore.Authentication.JwtBearer
- Microsoft.AspNetCore.Http.Abstractions
- Microsoft.Extensions.Configuration.Abstractions
- Microsoft.Extensions.DependencyInjection.Abstractions
- Microsoft.Extensions.Options
- Microsoft.Extensions.Logging.Abstractions
- Microsoft.Extensions.Hosting.Abstractions
- Making.Security

## License

This project is part of the Making framework.