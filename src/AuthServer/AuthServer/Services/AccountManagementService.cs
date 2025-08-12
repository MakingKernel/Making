using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using AuthServer.Models;
using Making.AspNetCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace AuthServer.Services;

/// <summary>
/// 账户管理服务 - 处理用户注册、登录、外部登录等操作
/// </summary>
[MiniApi(route: "/Account", Tags = "Account Management")]
[Filter(typeof(ApiResultFilter))]
public class AccountManagementService
{
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<AccountManagementService> _logger;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AccountManagementService(
        SignInManager<ApplicationUser> signInManager,
        UserManager<ApplicationUser> userManager,
        ILogger<AccountManagementService> logger,
        IHttpContextAccessor httpContextAccessor)
    {
        _signInManager = signInManager;
        _userManager = userManager;
        _logger = logger;
        _httpContextAccessor = httpContextAccessor;
    }


    /// <summary>
    /// 外部登录提供商
    /// </summary>
    [HttpGet("ExternalProviders")]
    public async Task<IEnumerable<ExternalProviderInfo>> GetExternalProviders()
    {
        var schemes = await _signInManager.GetExternalAuthenticationSchemesAsync();
        return schemes.Select(scheme => new ExternalProviderInfo
        {
            Name = scheme.Name.ToLowerInvariant(),
            DisplayName = scheme.DisplayName ?? scheme.Name,
            Provider = scheme.Name
        });
    }

    /// <summary>
    /// 外部登录
    /// </summary>
    [HttpGet("ExternalLogin")]
    public IResult ExternalLogin(string provider, string? returnUrl = null)
    {
        var redirectUrl = $"/Account/ExternalLoginCallback?returnUrl={Uri.EscapeDataString(returnUrl ?? "")}";
        
        var properties = _signInManager.ConfigureExternalAuthenticationProperties(
            provider, redirectUrl);
        
        return Results.Challenge(properties, new[] { provider });
    }

    /// <summary>
    /// 外部登录回调
    /// </summary>
    [HttpGet("ExternalLoginCallback")]
    public async Task<IResult> ExternalLoginCallback(string? returnUrl = null, string? remoteError = null)
    {
        if (remoteError != null)
        {
            _logger.LogError("外部登录错误: {Error}", remoteError);
            return Results.BadRequest(new { Error = $"外部提供商错误: {remoteError}" });
        }

        var info = await _signInManager.GetExternalLoginInfoAsync();
        if (info == null)
        {
            _logger.LogError("无法获取外部登录信息");
            return Results.BadRequest(new { Error = "外部登录信息获取失败" });
        }

        // 尝试使用外部登录信息登录
        var result = await _signInManager.ExternalLoginSignInAsync(
            info.LoginProvider, 
            info.ProviderKey,
            isPersistent: false, 
            bypassTwoFactor: true);

        if (result.Succeeded)
        {
            _logger.LogInformation("用户通过 {Provider} 外部登录成功", info.LoginProvider);
            return Results.Redirect(returnUrl ?? "/");
        }

        if (result.IsLockedOut)
        {
            return Results.BadRequest(new { Error = "账户已被锁定" });
        }

        // 如果用户还没有账户，尝试创建账户
        return await HandleNewExternalUser(info, returnUrl);
    }

    private async Task<IResult> HandleNewExternalUser(ExternalLoginInfo info, string? returnUrl)
    {
        var email = info.Principal.FindFirstValue(ClaimTypes.Email);
        if (string.IsNullOrEmpty(email))
        {
            return Results.BadRequest(new { Error = "无法从外部提供商获取邮箱地址" });
        }

        var user = await _userManager.FindByEmailAsync(email);
        
        if (user == null)
        {
            // 创建新用户
            user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                FirstName = info.Principal.FindFirstValue(ClaimTypes.GivenName),
                LastName = info.Principal.FindFirstValue(ClaimTypes.Surname),
                EmailConfirmed = true // 外部提供商已验证邮箱
            };

            var createResult = await _userManager.CreateAsync(user);
            if (!createResult.Succeeded)
            {
                var errors = createResult.Errors.Select(e => e.Description);
                return Results.BadRequest(new { Error = "创建用户失败", Details = errors });
            }

            _logger.LogInformation("通过 {Provider} 创建新用户 {Email}", info.LoginProvider, email);
        }

        // 关联外部登录
        await _userManager.AddLoginAsync(user, info);
        await _signInManager.SignInAsync(user, isPersistent: false);

        _logger.LogInformation("用户 {Email} 通过 {Provider} 登录成功", email, info.LoginProvider);
        
        return Results.Redirect(returnUrl ?? "/");
    }

    /// <summary>
    /// 注销
    /// </summary>
    [HttpPost("Logout")]
    [IgnoreAntiforgeryToken]
    public async Task<IResult> Logout()
    {
        await _signInManager.SignOutAsync();
        _logger.LogInformation("用户已注销");
        
        return Results.Ok(new { Success = true, Message = "注销成功" });
    }
}

/// <summary>
/// 外部提供商信息
/// </summary>
public class ExternalProviderInfo
{
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Provider { get; set; } = string.Empty;
}