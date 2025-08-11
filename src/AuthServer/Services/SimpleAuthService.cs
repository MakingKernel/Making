using AuthServer.Models;
using Making.AspNetCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace AuthServer.Services;

/// <summary>
/// 简化的认证服务 - 用于验证基本功能
/// </summary>
[MiniApi(route: "/api/auth", Tags = "Simple Authentication")]
[Filter(typeof(ApiResultFilter))]
public class SimpleAuthService
{
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<SimpleAuthService> _logger;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public SimpleAuthService(
        SignInManager<ApplicationUser> signInManager,
        UserManager<ApplicationUser> userManager,
        ILogger<SimpleAuthService> logger,
        IHttpContextAccessor httpContextAccessor)
    {
        _signInManager = signInManager;
        _userManager = userManager;
        _logger = logger;
        _httpContextAccessor = httpContextAccessor;
    }
    
    private HttpContext HttpContext => _httpContextAccessor.HttpContext ?? throw new InvalidOperationException("HttpContext is not available");

    /// <summary>
    /// 健康检查
    /// </summary>
    [HttpGet("health")]
    public IResult Health()
    {
        return Results.Ok(new 
        { 
            Status = "Healthy", 
            Message = "Simple Auth Service is running",
            Timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// 获取用户信息
    /// </summary>
    [HttpGet("me")]
    public async Task<IResult> GetCurrentUser()
    {
        var user = await _userManager.GetUserAsync(HttpContext.User);
        if (user == null)
        {
            return Results.Unauthorized();
        }

        return Results.Ok(new
        {
            Id = user.Id,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            IsActive = user.IsActive
        });
    }

    /// <summary>
    /// 注销
    /// </summary>
    [HttpPost("logout")]
    public async Task<IResult> Logout()
    {
        await _signInManager.SignOutAsync();
        _logger.LogInformation("User logged out.");
        
        return Results.Ok(new { Success = true, Message = "Logged out successfully" });
    }
}