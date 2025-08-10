using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using AuthServer.Data;
using AuthServer.Models;
using Making.AspNetCore;
using OpenIddict.Abstractions;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection"));
    options.UseOpenIddict();
});

builder.Services.AddMiniApis();
builder.Services.AddHttpContextAccessor();

// Add Identity services
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = false;
    options.Password.RequireLowercase = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.Password.RequiredLength = 6;
    options.User.RequireUniqueEmail = true;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

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

        options.AddEphemeralEncryptionKey()
               .AddEphemeralSigningKey();

        options.UseAspNetCore()
               .EnableTokenEndpointPassthrough()
               .EnableAuthorizationEndpointPassthrough();
    })
    .AddValidation(options =>
    {
        options.UseLocalServer();
        options.UseAspNetCore();
    });

// Add Authentication with external providers
builder.Services.AddAuthentication()
    .AddGitHub(options =>
    {
        options.ClientId = builder.Configuration["Authentication:GitHub:ClientId"]!;
        options.ClientSecret = builder.Configuration["Authentication:GitHub:ClientSecret"]!;
        options.CallbackPath = "/signin-github";
        options.Scope.Add("user:email");
    })
    .AddGitee(options =>
    {
        options.ClientId = builder.Configuration["Authentication:Gitee:ClientId"]!;
        options.ClientSecret = builder.Configuration["Authentication:Gitee:ClientSecret"]!;
        options.CallbackPath = "/signin-gitee";
    });

// Add Authorization services
builder.Services.AddAuthorization();

// 不需要控制器，使用MiniAPI

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseDeveloperExceptionPage();
}

app.MapMiniApis();

app.UseCors();

app.UseAuthentication();
app.UseAuthorization();


// Initialize database
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    var manager = scope.ServiceProvider.GetRequiredService<IOpenIddictApplicationManager>();
    
    await context.Database.EnsureCreatedAsync();
    
    // 创建默认客户端应用
    await CreateClientsAsync(manager);
    
    // 创建默认用户
    await CreateDefaultUserAsync(userManager);
}

app.Run();

static async Task CreateClientsAsync(IOpenIddictApplicationManager manager)
{
    if (await manager.FindByClientIdAsync("web-client") == null)
    {
        await manager.CreateAsync(new OpenIddictApplicationDescriptor
        {
            ClientId = "web-client",
            ClientSecret = "web-client-secret",
            DisplayName = "Web Client",
            Permissions =
            {
                OpenIddictConstants.Permissions.Endpoints.Authorization,
                OpenIddictConstants.Permissions.Endpoints.Token,
                OpenIddictConstants.Permissions.GrantTypes.AuthorizationCode,
                OpenIddictConstants.Permissions.GrantTypes.RefreshToken,
                OpenIddictConstants.Permissions.ResponseTypes.Code,
                OpenIddictConstants.Permissions.Scopes.Email,
                OpenIddictConstants.Permissions.Scopes.Profile,
                OpenIddictConstants.Permissions.Scopes.Roles
            },
            RedirectUris =
            {
                new Uri("https://localhost:5002/callback"),
                new Uri("http://localhost:5002/callback")
            },
            PostLogoutRedirectUris =
            {
                new Uri("https://localhost:5002/"),
                new Uri("http://localhost:5002/")
            }
        });
    }

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

        await userManager.CreateAsync(defaultUser, "Admin123!");
    }
}
