using AuthServer.Models;
using Making.AspNetCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace AuthServer.Services;

[MiniApi(route: "/api/auth", Tags = "Authentication")]
[Filter(typeof(ApiResultFilter))]
public class AuthService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;

    public AuthService(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager)
    {
        _userManager = userManager;
        _signInManager = signInManager;
    }

    [HttpPost("/api/auth/login")]
    public async Task<LoginResult> Login([FromBody] SimpleLoginRequest request)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user == null)
        {
            throw new UnauthorizedAccessException("Invalid email or password");
        }

        var result = await _signInManager.CheckPasswordSignInAsync(user, request.Password, false);
        if (!result.Succeeded)
        {
            throw new UnauthorizedAccessException("Invalid email or password");
        }

        return new LoginResult
        {
            Success = true,
            User = new UserInfo
            {
                Id = user.Id,
                Email = user.Email!,
                Name = user.UserName!,
                FirstName = user.FirstName,
                LastName = user.LastName
            }
        };
    }

    [HttpGet("/api/auth/providers")]
    public async Task<IEnumerable<AuthProviderInfo>> GetProviders()
    {
        // 获取配置的外部认证提供商
        var schemes = await _signInManager.GetExternalAuthenticationSchemesAsync();
        return schemes.Select(scheme => new AuthProviderInfo
        {
            Name = scheme.Name.ToLower(),
            DisplayName = scheme.DisplayName ?? scheme.Name,
            Provider = scheme.Name
        });
    }

    [HttpGet("/api/auth/user")]
    [Authorize]
    public async Task<UserInfo> GetCurrentUser([FromServices] IHttpContextAccessor httpContextAccessor)
    {
        var httpContext = httpContextAccessor.HttpContext;
        if (httpContext?.User == null)
        {
            throw new UnauthorizedAccessException("User not authenticated");
        }

        var user = await _userManager.GetUserAsync(httpContext.User);
        if (user == null)
        {
            throw new UnauthorizedAccessException("User not found");
        }

        return new UserInfo
        {
            Id = user.Id,
            Email = user.Email!,
            Name = user.UserName!,
            FirstName = user.FirstName,
            LastName = user.LastName
        };
    }
}

public class SimpleLoginRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class AuthProviderInfo
{
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Provider { get; set; } = string.Empty;
}

public class LoginResult
{
    public bool Success { get; set; }
    public UserInfo User { get; set; } = null!;
}

public class UserInfo
{
    public string Id { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
}