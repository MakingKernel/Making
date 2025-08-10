using System.Security.Claims;
using AuthServer.Models;
using Making.AspNetCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace AuthServer.Services;

[MiniApi(route: "/account", Tags = "Account Management")]
[Filter(typeof(ApiResultFilter))]
public class AccountService
{
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<AccountService> _logger;

    public AccountService(
        SignInManager<ApplicationUser> signInManager,
        UserManager<ApplicationUser> userManager,
        ILogger<AccountService> logger)
    {
        _signInManager = signInManager;
        _userManager = userManager;
        _logger = logger;
    }

    [HttpPost("register")]
    public async Task<IResult> Register(RegisterRequest request)
    {
        var user = new ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email,
            FirstName = request.FirstName,
            LastName = request.LastName
        };

        var result = await _userManager.CreateAsync(user, request.Password);

        if (result.Succeeded)
        {
            _logger.LogInformation("User created a new account with password.");
            
            return Results.Ok(new { Success = true, Message = "User registered successfully" });
        }

        return Results.BadRequest(new { Success = false, Errors = result.Errors.Select(e => e.Description) });
    }

    [HttpPost("login")]
    public async Task<IResult> Login(LoginRequest request)
    {
        var result = await _signInManager.PasswordSignInAsync(request.Email, request.Password, request.RememberMe, lockoutOnFailure: false);

        if (result.Succeeded)
        {
            _logger.LogInformation("User logged in.");
            
            var user = await _userManager.FindByEmailAsync(request.Email);
            
            return Results.Ok(new 
            { 
                Success = true, 
                Message = "Logged in successfully",
                User = new
                {
                    user!.Id,
                    user.Email,
                    user.FirstName,
                    user.LastName
                }
            });
        }

        if (result.IsLockedOut)
        {
            _logger.LogWarning("User account locked out.");
            return Results.BadRequest(new { Success = false, Message = "User account locked out." });
        }

        return Results.BadRequest(new { Success = false, Message = "Invalid login attempt." });
    }

    [HttpPost("logout")]
    public async Task<IResult> Logout()
    {
        await _signInManager.SignOutAsync();
        _logger.LogInformation("User logged out.");
        
        return Results.Ok(new { Success = true, Message = "Logged out successfully" });
    }

    [HttpGet("external-login")]
    public IResult ExternalLogin(string provider, string returnUrl = null!)
    {
        var redirectUrl = "/account/external-login-callback";
        if (!string.IsNullOrEmpty(returnUrl))
        {
            redirectUrl += $"?returnUrl={Uri.EscapeDataString(returnUrl)}";
        }
        
        var properties = _signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl);
        
        return Results.Challenge(properties, new[] { provider });
    }

    [HttpGet("external-login-callback")]
    public async Task<IResult> ExternalLoginCallback(string returnUrl = null!, string remoteError = null!)
    {
        if (remoteError != null)
        {
            return Results.BadRequest(new { Success = false, Message = $"Error from external provider: {remoteError}" });
        }

        var info = await _signInManager.GetExternalLoginInfoAsync();
        if (info == null)
        {
            return Results.BadRequest(new { Success = false, Message = "Error loading external login information." });
        }

        var result = await _signInManager.ExternalLoginSignInAsync(info.LoginProvider, info.ProviderKey, isPersistent: false, bypassTwoFactor: true);
        
        if (result.Succeeded)
        {
            _logger.LogInformation("{Name} logged in with {LoginProvider} provider.", info.Principal.Identity!.Name, info.LoginProvider);
            
            var user = await _userManager.FindByLoginAsync(info.LoginProvider, info.ProviderKey);
            
            return Results.Ok(new 
            { 
                Success = true, 
                Message = "External login successful",
                User = new
                {
                    user!.Id,
                    user.Email,
                    user.FirstName,
                    user.LastName
                }
            });
        }

        if (result.IsLockedOut)
        {
            return Results.BadRequest(new { Success = false, Message = "User account locked out." });
        }
        
        // 如果用户还没有账户，则创建一个
        var email = info.Principal.FindFirstValue(ClaimTypes.Email);
        var firstName = info.Principal.FindFirstValue(ClaimTypes.GivenName);
        var lastName = info.Principal.FindFirstValue(ClaimTypes.Surname);
        var name = info.Principal.FindFirstValue(ClaimTypes.Name);
        
        if (email != null)
        {
            var user = await _userManager.FindByEmailAsync(email);
            
            if (user == null)
            {
                user = new ApplicationUser
                {
                    UserName = email,
                    Email = email,
                    FirstName = firstName,
                    LastName = lastName,
                    EmailConfirmed = true
                };

                var createResult = await _userManager.CreateAsync(user);
                if (createResult.Succeeded)
                {
                    await _userManager.AddLoginAsync(user, info);
                    await _signInManager.SignInAsync(user, isPersistent: false, info.LoginProvider);
                    
                    _logger.LogInformation("User created an account using {Name} provider.", info.LoginProvider);
                    
                    return Results.Ok(new 
                    { 
                        Success = true, 
                        Message = "External login successful - new account created",
                        User = new
                        {
                            user.Id,
                            user.Email,
                            user.FirstName,
                            user.LastName
                        }
                    });
                }
                
                return Results.BadRequest(new { Success = false, Errors = createResult.Errors.Select(e => e.Description) });
            }
            
            // 用户存在但没有关联外部登录
            await _userManager.AddLoginAsync(user, info);
            await _signInManager.SignInAsync(user, isPersistent: false, info.LoginProvider);
            
            return Results.Ok(new 
            { 
                Success = true, 
                Message = "External login successful - account linked",
                User = new
                {
                    user.Id,
                    user.Email,
                    user.FirstName,
                    user.LastName
                }
            });
        }

        return Results.BadRequest(new { Success = false, Message = "Unable to get email from external provider." });
    }

    [HttpGet("providers")]
    public async Task<IEnumerable<ExternalProviderInfo>> GetExternalProviders()
    {
        var schemes = await _signInManager.GetExternalAuthenticationSchemesAsync();
        return schemes.Select(scheme => new ExternalProviderInfo
        {
            Name = scheme.Name.ToLower(),
            DisplayName = scheme.DisplayName ?? scheme.Name,
            Provider = scheme.Name
        });
    }
}

public class RegisterRequest
{
    public string Email { get; set; } = null!;
    public string Password { get; set; } = null!;
    public string FirstName { get; set; } = null!;
    public string LastName { get; set; } = null!;
}

public class LoginRequest
{
    public string Email { get; set; } = null!;
    public string Password { get; set; } = null!;
    public bool RememberMe { get; set; }
}

public class ExternalProviderInfo
{
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Provider { get; set; } = string.Empty;
}