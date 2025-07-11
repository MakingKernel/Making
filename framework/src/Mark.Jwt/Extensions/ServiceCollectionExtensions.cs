using System.Text;
using Mark.Jwt.Options;
using Mark.Jwt.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Mark.Jwt.Extensions;

/// <summary>
/// JWT服务注册扩展方法
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// 添加JWT服务
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="configuration">配置</param>
    /// <returns></returns>
    public static IServiceCollection AddJwt(this IServiceCollection services, IConfiguration configuration)
    {
        return services.AddJwt(configuration.GetSection(JwtOptions.SectionName));
    }

    /// <summary>
    /// 添加JWT服务
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="configurationSection">JWT配置节</param>
    /// <returns></returns>
    public static IServiceCollection AddJwt(this IServiceCollection services, IConfigurationSection configurationSection)
    {
        services.Configure<JwtOptions>(configurationSection);
        services.AddScoped<IJwtService, JwtService>();

        // 添加默认的内存刷新令牌存储
        services.AddSingleton<IRefreshTokenStore, InMemoryRefreshTokenStore>();

        return services;
    }

    /// <summary>
    /// 添加JWT服务
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="configureOptions">配置选项委托</param>
    /// <returns></returns>
    public static IServiceCollection AddJwt(this IServiceCollection services, Action<JwtOptions> configureOptions)
    {
        services.Configure(configureOptions);
        services.AddScoped<IJwtService, JwtService>();

        // 添加默认的内存刷新令牌存储
        services.AddSingleton<IRefreshTokenStore, InMemoryRefreshTokenStore>();

        return services;
    }

    /// <summary>
    /// 添加JWT服务（带自定义刷新令牌存储）
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="configureOptions">配置选项委托</param>
    /// <param name="refreshTokenStoreLifetime">刷新令牌存储生命周期</param>
    /// <returns></returns>
    public static IServiceCollection AddJwt<TRefreshTokenStore>(this IServiceCollection services,
        Action<JwtOptions> configureOptions,
        ServiceLifetime refreshTokenStoreLifetime = ServiceLifetime.Scoped)
        where TRefreshTokenStore : class, IRefreshTokenStore
    {
        services.Configure(configureOptions);
        services.AddScoped<IJwtService, JwtService>();

        // 添加自定义刷新令牌存储
        services.Add(new ServiceDescriptor(typeof(IRefreshTokenStore), typeof(TRefreshTokenStore), refreshTokenStoreLifetime));

        return services;
    }

    /// <summary>
    /// 添加JWT认证
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="configuration">配置</param>
    /// <returns></returns>
    public static IServiceCollection AddJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        return services.AddJwtAuthentication(configuration.GetSection(JwtOptions.SectionName));
    }

    /// <summary>
    /// 添加JWT认证
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="configurationSection">JWT配置节</param>
    /// <returns></returns>
    public static IServiceCollection AddJwtAuthentication(this IServiceCollection services, IConfigurationSection configurationSection)
    {
        var jwtOptions = new JwtOptions();
        configurationSection.Bind(jwtOptions);
        
        return services.AddJwtAuthentication(jwtOptions);
    }

    /// <summary>
    /// 添加JWT认证
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="jwtOptions">JWT选项</param>
    /// <returns></returns>
    public static IServiceCollection AddJwtAuthentication(this IServiceCollection services, JwtOptions jwtOptions)
    {
        var key = Encoding.UTF8.GetBytes(jwtOptions.SecretKey);
        var securityKey = new SymmetricSecurityKey(key);

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.RequireHttpsMetadata = jwtOptions.RequireHttpsMetadata;
            options.SaveToken = jwtOptions.SaveToken;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = jwtOptions.ValidateIssuer,
                ValidateAudience = jwtOptions.ValidateAudience,
                ValidateLifetime = jwtOptions.ValidateLifetime,
                ValidateIssuerSigningKey = jwtOptions.ValidateIssuerSigningKey,
                ValidIssuer = jwtOptions.Issuer,
                ValidAudience = jwtOptions.Audience,
                IssuerSigningKey = securityKey,
                ClockSkew = TimeSpan.FromSeconds(jwtOptions.ClockSkewSeconds),
                RequireExpirationTime = true
            };

            // 应用额外的验证参数
            foreach (var parameter in jwtOptions.AdditionalValidationParameters)
            {
                var property = typeof(TokenValidationParameters).GetProperty(parameter.Key);
                if (property != null && property.CanWrite)
                {
                    property.SetValue(options.TokenValidationParameters, parameter.Value);
                }
            }
        });

        return services;
    }

    /// <summary>
    /// 添加完整的JWT支持（包括服务和认证）
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="configuration">配置</param>
    /// <returns></returns>
    public static IServiceCollection AddJwtSupport(this IServiceCollection services, IConfiguration configuration)
    {
        return services
            .AddJwt(configuration)
            .AddJwtAuthentication(configuration);
    }

    /// <summary>
    /// 添加完整的JWT支持（包括服务和认证）
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="configurationSection">JWT配置节</param>
    /// <returns></returns>
    public static IServiceCollection AddJwtSupport(this IServiceCollection services, IConfigurationSection configurationSection)
    {
        return services
            .AddJwt(configurationSection)
            .AddJwtAuthentication(configurationSection);
    }

    /// <summary>
    /// 添加完整的JWT支持（包括服务和认证）
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="configureOptions">配置选项委托</param>
    /// <returns></returns>
    public static IServiceCollection AddJwtSupport(this IServiceCollection services, Action<JwtOptions> configureOptions)
    {
        var jwtOptions = new JwtOptions();
        configureOptions(jwtOptions);

        return services
            .AddJwt(configureOptions)
            .AddJwtAuthentication(jwtOptions);
    }

    /// <summary>
    /// 添加完整的JWT支持（带自定义刷新令牌存储）
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="configureOptions">配置选项委托</param>
    /// <param name="refreshTokenStoreLifetime">刷新令牌存储生命周期</param>
    /// <returns></returns>
    public static IServiceCollection AddJwtSupport<TRefreshTokenStore>(this IServiceCollection services,
        Action<JwtOptions> configureOptions,
        ServiceLifetime refreshTokenStoreLifetime = ServiceLifetime.Scoped)
        where TRefreshTokenStore : class, IRefreshTokenStore
    {
        var jwtOptions = new JwtOptions();
        configureOptions(jwtOptions);

        return services
            .AddJwt<TRefreshTokenStore>(configureOptions, refreshTokenStoreLifetime)
            .AddJwtAuthentication(jwtOptions);
    }

    /// <summary>
    /// 验证JWT配置
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <returns></returns>
    public static IServiceCollection ValidateJwtOptions(this IServiceCollection services)
    {
        services.AddOptions<JwtOptions>()
            .PostConfigure(options =>
            {
                var errors = options.Validate().ToList();
                if (errors.Count > 0)
                {
                    throw new InvalidOperationException($"JWT configuration is invalid: {string.Join(", ", errors)}");
                }
            });

        return services;
    }

    /// <summary>
    /// 添加JWT后台服务（用于清理过期令牌等）
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <returns></returns>
    public static IServiceCollection AddJwtBackgroundServices(this IServiceCollection services)
    {
        // 这里可以添加后台服务来定期清理过期的刷新令牌
        // services.AddHostedService<JwtCleanupService>();

        return services;
    }

    /// <summary>
    /// 添加JWT服务（仅服务，不包含认证配置）
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="configureOptions">配置选项委托</param>
    /// <returns></returns>
    public static IServiceCollection AddJwtServices(this IServiceCollection services, Action<JwtOptions> configureOptions)
    {
        services.Configure(configureOptions);
        services.AddScoped<IJwtService, JwtService>();

        // 添加默认的内存刷新令牌存储
        services.AddSingleton<IRefreshTokenStore, InMemoryRefreshTokenStore>();

        return services;
    }

    /// <summary>
    /// 添加JWT服务（仅服务，不包含认证配置）
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="configuration">配置</param>
    /// <returns></returns>
    public static IServiceCollection AddJwtServices(this IServiceCollection services, IConfiguration configuration)
    {
        return services.AddJwtServices(configuration.GetSection(JwtOptions.SectionName));
    }

    /// <summary>
    /// 添加JWT服务（仅服务，不包含认证配置）
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="configurationSection">JWT配置节</param>
    /// <returns></returns>
    public static IServiceCollection AddJwtServices(this IServiceCollection services, IConfigurationSection configurationSection)
    {
        services.Configure<JwtOptions>(configurationSection);
        services.AddScoped<IJwtService, JwtService>();

        // 添加默认的内存刷新令牌存储
        services.AddSingleton<IRefreshTokenStore, InMemoryRefreshTokenStore>();

        return services;
    }
}
