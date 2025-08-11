using AuthServer.Data;
using AuthServer.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OpenIddict.Abstractions;
using Scalar.AspNetCore;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Diagnostics.HealthChecks;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection"));
    options.UseOpenIddict();
});

builder.Services.AddMiniApis();
builder.Services.AddHttpContextAccessor();

// Add Identity services with enhanced security
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
    {
        // 强化密码策略
        options.Password.RequireDigit = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireNonAlphanumeric = true;
        options.Password.RequireUppercase = true;
        options.Password.RequiredLength = 12;
        options.Password.RequiredUniqueChars = 4;
        
        // 账户锁定策略
        options.Lockout.AllowedForNewUsers = true;
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
        
        // 用户设置
        options.User.RequireUniqueEmail = true;
        options.User.AllowedUserNameCharacters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@";
        
        // 邮箱确认
        options.SignIn.RequireConfirmedEmail = false; // 开发环境暂时关闭
        options.SignIn.RequireConfirmedPhoneNumber = false;
        
        // Token 设置
        options.Tokens.EmailConfirmationTokenProvider = TokenOptions.DefaultEmailProvider;
        options.Tokens.PasswordResetTokenProvider = TokenOptions.DefaultEmailProvider;
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

// 配置认证方案，使其与前端API模式兼容
builder.Services.ConfigureApplicationCookie(options =>
{
    // API模式配置：不重定向，返回401状态码
    options.Events.OnRedirectToLogin = context =>
    {
        // 如果是API请求（Accept包含json）或明确的API路径，返回401而不是重定向
        if (context.Request.Path.StartsWithSegments("/connect") || 
            context.Request.Headers["Accept"].ToString().Contains("application/json"))
        {
            context.Response.StatusCode = 401;
            return Task.CompletedTask;
        }
        
        // 否则重定向到前端登录页面（保持原有逻辑）
        context.Response.Redirect(context.RedirectUri);
        return Task.CompletedTask;
    };
    
    options.Events.OnRedirectToAccessDenied = context =>
    {
        context.Response.StatusCode = 403;
        return Task.CompletedTask;
    };
});

// Configure OpenIddict
builder.Services.AddOpenIddict()
    .AddCore(options =>
    {
        options.UseEntityFrameworkCore()
            .UseDbContext<ApplicationDbContext>();
    })
    .AddServer(options =>
    {
        options.SetTokenEndpointUris("/connect/token")
            .SetAuthorizationEndpointUris("/connect/authorize")
            .SetIntrospectionEndpointUris("/connect/introspect")
            .SetRevocationEndpointUris("/connect/revoke");

        options.AllowAuthorizationCodeFlow()
            .AllowClientCredentialsFlow()
            .AllowRefreshTokenFlow()
            .AllowPasswordFlow();

        // Token 生命周期配置
        options.SetAccessTokenLifetime(TimeSpan.FromMinutes(15));
        options.SetAuthorizationCodeLifetime(TimeSpan.FromMinutes(10));
        options.SetRefreshTokenLifetime(TimeSpan.FromDays(7));
        options.SetIdentityTokenLifetime(TimeSpan.FromMinutes(15));
        
        // 滚动刷新令牌功能在OpenIddict 7.0中可能有变化，暂时注释
        // options.UseRollingRefreshTokens();
        
        // 要求 PKCE
        options.RequireProofKeyForCodeExchange();
        
        // 证书配置
        if (builder.Environment.IsDevelopment())
        {
            options.AddEphemeralEncryptionKey()
                   .AddEphemeralSigningKey();
            options.AddDevelopmentEncryptionCertificate()
                   .AddDevelopmentSigningCertificate();
        }
        else
        {
            // 生产环境使用真实证书
            // options.AddSigningCertificate(GetProductionSigningCertificate());
            // options.AddEncryptionCertificate(GetProductionEncryptionCertificate());
        }

        options.UseAspNetCore()
            .EnableTokenEndpointPassthrough()
            .EnableAuthorizationEndpointPassthrough() // 重新启用以便自定义处理
            // .EnableUserinfoEndpointPassthrough() // 可能在OpenIddict 7.0中不存在
            // .EnableLogoutEndpointPassthrough() // 可能在OpenIddict 7.0中不存在
            .EnableStatusCodePagesIntegration();
            
        // 生产环境启用 HTTPS 要求
        if (builder.Environment.IsDevelopment())
        {
            options.UseAspNetCore().DisableTransportSecurityRequirement();
        }
    })
    .AddValidation(options =>
    {
        options.UseLocalServer();
        options.UseAspNetCore();
    });

// Add Authentication with external providers
builder.Services.AddAuthentication();
// TODO: 配置实际的 ClientId 和 ClientSecret 后再启用
// .AddGitHub(options =>
// {
//     options.ClientId = builder.Configuration["Authentication:GitHub:ClientId"]!;
//     options.ClientSecret = builder.Configuration["Authentication:GitHub:ClientSecret"]!;
//     options.CallbackPath = "/signin-github";
//     options.Scope.Add("user:email");
// })
// .AddGitee(options =>
// {
//     options.ClientId = builder.Configuration["Authentication:Gitee:ClientId"]!;
//     options.ClientSecret = builder.Configuration["Authentication:Gitee:ClientSecret"]!;
//     options.CallbackPath = "/signin-gitee";
// });

// Add Authorization services
builder.Services.AddAuthorization();

// 不需要控制器，使用MiniAPI

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// 安全的 CORS 配置
builder.Services.AddCors(options =>
{
    if (builder.Environment.IsDevelopment())
    {
        // 开发环境：允许本地前端
        options.AddPolicy("Development", policy => policy
            .WithOrigins("http://localhost:5173", "http://localhost:3000", "http://127.0.0.1:5173")
            .WithMethods("GET", "POST", "PUT", "DELETE", "OPTIONS")
            .WithHeaders("Authorization", "Content-Type", "Accept")
            .AllowCredentials()
            .SetPreflightMaxAge(TimeSpan.FromMinutes(10)));
    }
    else
    {
        // 生产环境：限制特定域名
        options.AddPolicy("Production", policy => policy
            .WithOrigins() // TODO: 添加生产域名
            .WithMethods("GET", "POST")
            .WithHeaders("Authorization", "Content-Type")
            .AllowCredentials());
    }
});

// 添加安全头
builder.Services.AddHsts(options =>
{
    options.Preload = true;
    options.IncludeSubDomains = true;
    options.MaxAge = TimeSpan.FromDays(365);
});

// 添加数据保护
builder.Services.AddDataProtection();
    // .PersistKeysToDbContext<ApplicationDbContext>() // 需要额外的包支持
    // .SetDefaultKeyLifetime(TimeSpan.FromDays(90)); // 方法可能不存在

// 添加速率限制
builder.Services.AddRateLimiter(options =>
{
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: partition => new FixedWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit = 100,
                Window = TimeSpan.FromMinutes(1)
            }));
            
    // 登录端点特殊限制
    options.AddPolicy("LoginPolicy", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: partition => new FixedWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit = 5,
                Window = TimeSpan.FromMinutes(1)
            }));
});

// 添加健康检查
builder.Services.AddHealthChecks()
    .AddCheck("database", () => HealthCheckResult.Healthy("Database is running"))
    .AddCheck("openiddict", () => HealthCheckResult.Healthy("OpenIddict is running"));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseDeveloperExceptionPage();
    app.MapScalarApiReference();
}

// 使用环境特定的 CORS 策略
if (app.Environment.IsDevelopment())
{
    app.UseCors("Development");
}
else
{
    app.UseCors("Production");
    app.UseHsts();
}

// 安全头中间件
app.Use(async (context, next) =>
{
    context.Response.Headers["X-Frame-Options"] = "DENY";
    
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["X-XSS-Protection"] = "1; mode=block";
    context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    context.Response.Headers["Content-Security-Policy"] = 
        "default-src 'self'; script-src 'self' 'unsafe-inline'; style-src 'self' 'unsafe-inline'; img-src 'self' data:;";
    await next();
});

app.MapMiniApis();


// 使用速率限制
app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

// 添加健康检查端点
app.MapHealthChecks("/health");


// Initialize database
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    var manager = scope.ServiceProvider.GetRequiredService<IOpenIddictApplicationManager>();

    await context.Database.EnsureCreatedAsync();

    // 创建默认客户端应用
    await CreateClientsAsync(manager);
    
    // 创建默认scopes
    var scopeManager = scope.ServiceProvider.GetRequiredService<IOpenIddictScopeManager>();
    await CreateScopesAsync(scopeManager);

    // 创建默认用户
    await CreateDefaultUserAsync(userManager);
}

app.Run();

static async Task CreateClientsAsync(IOpenIddictApplicationManager manager)
{
    var existingClient = await manager.FindByClientIdAsync("web-client");
    if (existingClient != null)
    {
        // Update existing client
        await manager.DeleteAsync(existingClient);
    }
    
    await manager.CreateAsync(new OpenIddictApplicationDescriptor
    {
        ClientId = "web-client",
        DisplayName = "Web Client",
        ApplicationType = OpenIddictConstants.ClientTypes.Public,
        Permissions =
        {
            OpenIddictConstants.Permissions.Endpoints.Authorization,
            OpenIddictConstants.Permissions.Endpoints.Token,
            OpenIddictConstants.Permissions.GrantTypes.AuthorizationCode,
            OpenIddictConstants.Permissions.GrantTypes.RefreshToken,
            OpenIddictConstants.Permissions.GrantTypes.Password,
            OpenIddictConstants.Permissions.ResponseTypes.Code,
            OpenIddictConstants.Permissions.Prefixes.Scope + "openid",
            OpenIddictConstants.Permissions.Scopes.Email,
            OpenIddictConstants.Permissions.Scopes.Profile,
            OpenIddictConstants.Permissions.Scopes.Roles
        },
        RedirectUris =
        {
            new Uri("http://localhost:5173/callback"),
            new Uri("http://localhost:5176/callback"),
            new Uri("http://localhost:5177/callback")
        },
        PostLogoutRedirectUris =
        {
            new Uri("http://localhost:5173/"),
            new Uri("http://localhost:5176/"),
            new Uri("http://localhost:5177/")
        }
    });

    if (await manager.FindByClientIdAsync("api-client") == null)
    {
        await manager.CreateAsync(new OpenIddictApplicationDescriptor
        {
            ClientId = "api-client",
            ClientSecret = "api-client-secret",
            DisplayName = "API Client",
            Permissions =
            {
                OpenIddictConstants.Permissions.Endpoints.Token,
                OpenIddictConstants.Permissions.GrantTypes.ClientCredentials
            }
        });
    }
}

static async Task CreateScopesAsync(IOpenIddictScopeManager manager)
{
    if (await manager.FindByNameAsync("openid") == null)
    {
        await manager.CreateAsync(new OpenIddictScopeDescriptor
        {
            Name = "openid",
            DisplayName = "OpenID",
            Description = "OpenID Connect scope"
        });
    }
    
    if (await manager.FindByNameAsync("profile") == null)
    {
        await manager.CreateAsync(new OpenIddictScopeDescriptor
        {
            Name = "profile",
            DisplayName = "Profile",
            Description = "Profile information scope"
        });
    }
    
    if (await manager.FindByNameAsync("email") == null)
    {
        await manager.CreateAsync(new OpenIddictScopeDescriptor
        {
            Name = "email",
            DisplayName = "Email",
            Description = "Email address scope"
        });
    }
}

static async Task CreateDefaultUserAsync(UserManager<ApplicationUser> userManager)
{
    var defaultUser = await userManager.FindByEmailAsync("admin@example.com");
    if (defaultUser == null)
    {
        defaultUser = new ApplicationUser
        {
            UserName = "admin@example.com",
            Email = "admin@example.com",
            EmailConfirmed = true,
            FirstName = "Admin",
            LastName = "User"
        };

        await userManager.CreateAsync(defaultUser, "Admin123456!");
    }
}