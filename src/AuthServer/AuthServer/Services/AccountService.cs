using System.Security.Claims;
using AuthServer.Models;
using Making.AspNetCore;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using OpenIddict.Abstractions;
using Microsoft.AspNetCore.Authentication;
using OpenIddict.Server.AspNetCore;
using Microsoft.IdentityModel.Tokens;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace AuthServer.Services;

/// <summary>
/// OAuth2/OIDC 认证端点处理器 - 遵循标准协议流程
/// </summary>
[MiniApi(route: "/connect", Tags = "OAuth2/OIDC Endpoints")]
[Filter(typeof(ApiResultFilter))]
public class AuthenticationService
{
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<AuthenticationService> _logger;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AuthenticationService(
        SignInManager<ApplicationUser> signInManager,
        UserManager<ApplicationUser> userManager,
        ILogger<AuthenticationService> logger,
        IHttpContextAccessor httpContextAccessor)
    {
        _signInManager = signInManager;
        _userManager = userManager;
        _logger = logger;
        _httpContextAccessor = httpContextAccessor;
    }
    
    private HttpContext HttpContext => _httpContextAccessor.HttpContext ?? throw new InvalidOperationException("HttpContext is not available");

    /// <summary>
    /// 简化的健康检查端点
    /// </summary>
    [HttpGet("health")]
    public IResult Health()
    {
        return Results.Ok(new { Status = "Healthy", Message = "Authentication service is running" });
    }

    /// <summary>
    /// 临时占位方法 - Token端点将由OpenIddict自动处理
    /// </summary>
    [HttpGet("info")]
    public IResult Info()
    {
        return Results.Ok(new 
        { 
            Service = "AuthenticationService", 
            Message = "OpenIddict handles token endpoints automatically",
            Endpoints = new[] { "/connect/authorize", "/connect/token", "/connect/userinfo" }
        });
    }

    /// <summary>
    /// 简化的用户信息端点
    /// </summary>
    [HttpGet("user")]
    public async Task<IResult> GetUserInfo()
    {
        var user = await _userManager.GetUserAsync(HttpContext.User);
        if (user == null)
        {
            return Results.Unauthorized();
        }

        var userInfo = new
        {
            Id = user.Id,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Avatar = user.Avatar,
            IsActive = user.IsActive,
            EmailConfirmed = user.EmailConfirmed
        };

        return Results.Ok(userInfo);
    }

    /// <summary>
    /// 简单的注销端点
    /// </summary>
    [HttpPost("logout")]
    public async Task<IResult> Logout()
    {
        await _signInManager.SignOutAsync();
        _logger.LogInformation("User logged out.");
        
        return Results.Ok(new { Success = true, Message = "Logged out successfully" });
    }

    /// <summary>
    /// OAuth2/OIDC 授权端点 - 为前端API模式设计
    /// </summary>
    [HttpGet("authorize")]
    [HttpPost("authorize")]
    public async Task<IResult> Authorize()
    {
        // 获取OpenIddict请求上下文
        var context = HttpContext.GetOpenIddictServerRequest() ??
            throw new InvalidOperationException("无法获取OpenIddict服务器请求。");

        // 检查用户是否已认证
        if (!HttpContext.User.Identity.IsAuthenticated)
        {
            // API模式：返回401状态，包含授权参数，让前端处理认证
            return Results.Json(new 
            { 
                error = "unauthorized",
                error_description = "用户认证是必需的",
                authorization_request = new {
                    client_id = context.ClientId,
                    response_type = context.ResponseType,
                    redirect_uri = context.RedirectUri,
                    scope = context.GetScopes(),
                    state = context.State,
                    code_challenge = context.CodeChallenge,
                    code_challenge_method = context.CodeChallengeMethod
                },
                auth_endpoint = "/connect/authenticate",
                message = "请先进行身份验证，然后重新发起授权请求"
            }, statusCode: 401);
        }

        // 用户已认证，获取用户信息
        var user = await _userManager.GetUserAsync(HttpContext.User);
        if (user == null)
        {
            return Results.Problem("无法获取用户信息", statusCode: 500);
        }

        // 创建身份声明
        var identity = new ClaimsIdentity(
            authenticationType: TokenValidationParameters.DefaultAuthenticationType,
            nameType: Claims.Name,
            roleType: Claims.Role);

        // 添加必要的声明
        identity.AddClaim(new Claim(Claims.Subject, user.Id));
        identity.AddClaim(new Claim(Claims.Email, user.Email ?? ""));
        identity.AddClaim(new Claim(Claims.Name, $"{user.FirstName} {user.LastName}".Trim()));
        identity.AddClaim(new Claim(Claims.GivenName, user.FirstName ?? ""));
        identity.AddClaim(new Claim(Claims.FamilyName, user.LastName ?? ""));

        // 设置作用域和资源
        identity.SetScopes(context.GetScopes());
        identity.SetDestinations(GetDestinations);

        var principal = new ClaimsPrincipal(identity);

        // 返回授权响应
        return Results.SignIn(principal, authenticationScheme: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    }

    /// <summary>
    /// OAuth2认证端点 - 用于OAuth2流程中的用户认证
    /// </summary>
    [HttpPost("authenticate")]
    public async Task<IResult> Authenticate([FromBody] AuthenticateRequest request)
    {
        if (string.IsNullOrEmpty(request.Email) || string.IsNullOrEmpty(request.Password))
        {
            return Results.BadRequest(new { error = "invalid_request", error_description = "邮箱和密码不能为空" });
        }

        var result = await _signInManager.PasswordSignInAsync(
            request.Email,
            request.Password,
            request.RememberMe,
            lockoutOnFailure: true);

        if (result.Succeeded)
        {
            _logger.LogInformation("用户 {Email} 通过OAuth2流程成功认证", request.Email);
            
            return Results.Ok(new
            {
                success = true,
                message = "认证成功，请重新发起授权请求",
                next_step = "重新调用 /connect/authorize"
            });
        }

        if (result.IsLockedOut)
        {
            _logger.LogWarning("用户账户 {Email} 被锁定", request.Email);
            return Results.BadRequest(new { error = "account_locked", error_description = "账户已被锁定，请稍后再试" });
        }

        if (result.RequiresTwoFactor)
        {
            return Results.BadRequest(new { error = "two_factor_required", error_description = "需要两步验证" });
        }

        _logger.LogWarning("用户 {Email} OAuth2认证失败", request.Email);
        return Results.BadRequest(new { error = "invalid_credentials", error_description = "邮箱或密码错误" });
    }

    /// <summary>
    /// 确定声明的目标位置（访问令牌或身份令牌）
    /// </summary>
    private static IEnumerable<string> GetDestinations(Claim claim)
    {
        switch (claim.Type)
        {
            case Claims.Name:
            case Claims.GivenName:
            case Claims.FamilyName:
                yield return Destinations.IdentityToken;
                yield return Destinations.AccessToken;
                yield break;

            case Claims.Email:
                yield return Destinations.IdentityToken;
                yield return Destinations.AccessToken;
                yield break;

            case Claims.Role:
                yield return Destinations.AccessToken;
                yield break;

            case "AspNet.Identity.SecurityStamp":
                // 安全戳不应包含在令牌中
                yield break;

            default:
                yield return Destinations.AccessToken;
                yield break;
        }
    }

}

/// <summary>
/// OAuth2认证请求模型
/// </summary>
public class AuthenticateRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public bool RememberMe { get; set; } = false;
}
