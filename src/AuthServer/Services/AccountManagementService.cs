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
    /// 检查当前用户认证状态 - API模式，不重定向
    /// </summary>
    [HttpGet("Login")]
    public async Task<IResult> GetAuthenticationStatus(string? returnUrl = null)
    {
        // 检查当前用户是否已认证
        var user = await _userManager.GetUserAsync(_httpContextAccessor.HttpContext?.User);
        
        if (user != null)
        {
            return Results.Ok(new 
            { 
                IsAuthenticated = true,
                User = new {
                    Id = user.Id,
                    Email = user.Email,
                    FirstName = user.FirstName,
                    LastName = user.LastName
                },
                ReturnUrl = returnUrl 
            });
        }

        return Results.Ok(new 
        { 
            IsAuthenticated = false,
            LoginRequired = true,
            ReturnUrl = returnUrl,
            Message = "需要登录以继续OAuth2授权流程"
        });
    }

    /// <summary>
    /// 处理登录表单提交
    /// </summary>
    [HttpPost("Login")]
    [IgnoreAntiforgeryToken]
    public async Task<IResult> Login(LoginRequest request)
    {
        // 手动验证请求
        if (string.IsNullOrEmpty(request.Email) || string.IsNullOrEmpty(request.Password))
        {
            return Results.BadRequest(new { Error = "邮箱和密码不能为空" });
        }

        var result = await _signInManager.PasswordSignInAsync(
            request.Email, 
            request.Password, 
            request.RememberMe,
            lockoutOnFailure: true);

        if (result.Succeeded)
        {
            _logger.LogInformation("用户 {Email} 成功登录", request.Email);
            
            // OAuth2 流程：登录成功后重定向回授权端点
            if (!string.IsNullOrEmpty(request.ReturnUrl))
            {
                return Results.Redirect(request.ReturnUrl);
            }
            
            return Results.Redirect("/connect/authorize");
        }

        if (result.IsLockedOut)
        {
            _logger.LogWarning("用户账户 {Email} 被锁定", request.Email);
            return Results.BadRequest(new { Error = "账户已被锁定，请稍后再试" });
        }

        if (result.RequiresTwoFactor)
        {
            return Results.BadRequest(new { Error = "需要两步验证" });
        }

        _logger.LogWarning("用户 {Email} 登录失败", request.Email);
        return Results.BadRequest(new { Error = "邮箱或密码错误" });
    }

    /// <summary>
    /// 用户注册
    /// </summary>
    [HttpPost("Register")]
    [IgnoreAntiforgeryToken]
    public async Task<IResult> Register(RegisterRequest request)
    {
        // 手动验证请求
        if (string.IsNullOrEmpty(request.Email) || string.IsNullOrEmpty(request.Password) ||
            string.IsNullOrEmpty(request.FirstName) || string.IsNullOrEmpty(request.LastName))
        {
            return Results.BadRequest(new { Error = "所有字段都是必需的" });
        }

        // 检查邮箱是否已存在
        var existingUser = await _userManager.FindByEmailAsync(request.Email);
        if (existingUser != null)
        {
            return Results.BadRequest(new { Error = "该邮箱已被注册" });
        }

        var user = new ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email,
            FirstName = request.FirstName,
            LastName = request.LastName,
            EmailConfirmed = false // 需要邮箱验证
        };

        var result = await _userManager.CreateAsync(user, request.Password);

        if (result.Succeeded)
        {
            _logger.LogInformation("用户 {Email} 注册成功", request.Email);

            // 发送邮箱确认链接 (TODO: 实现邮件服务)
            // var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            // await _emailService.SendEmailConfirmationAsync(user, code);

            return Results.Ok(new 
            { 
                Success = true, 
                Message = "注册成功，请检查邮箱并确认您的账户" 
            });
        }

        var errors = result.Errors.Select(e => e.Description);
        return Results.BadRequest(new { Error = "注册失败", Details = errors });
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
/// 登录请求模型
/// </summary>
public class LoginRequest
{
    [Required(ErrorMessage = "邮箱地址不能为空")]
    [EmailAddress(ErrorMessage = "请输入有效的邮箱地址")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "密码不能为空")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    public bool RememberMe { get; set; }

    public string? ReturnUrl { get; set; }
}

/// <summary>
/// 注册请求模型
/// </summary>
public class RegisterRequest
{
    [Required(ErrorMessage = "邮箱地址不能为空")]
    [EmailAddress(ErrorMessage = "请输入有效的邮箱地址")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "密码不能为空")]
    [StringLength(100, ErrorMessage = "密码长度至少 {2} 个字符", MinimumLength = 12)]
    [DataType(DataType.Password)]
    [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^\da-zA-Z]).{12,}$", 
        ErrorMessage = "密码必须包含大小写字母、数字和特殊字符")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "确认密码不能为空")]
    [DataType(DataType.Password)]
    [Compare("Password", ErrorMessage = "密码和确认密码不匹配")]
    public string ConfirmPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "名字不能为空")]
    [StringLength(50, ErrorMessage = "名字长度不能超过50个字符")]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "姓氏不能为空")]
    [StringLength(50, ErrorMessage = "姓氏长度不能超过50个字符")]
    public string LastName { get; set; } = string.Empty;
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