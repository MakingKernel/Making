using Microsoft.IdentityModel.Tokens;

namespace Making.Jwt.Options;

/// <summary>
/// JWT配置选项
/// </summary>
public class JwtOptions
{
    /// <summary>
    /// 配置节名称
    /// </summary>
    public const string SectionName = "Jwt";

    /// <summary>
    /// 密钥
    /// </summary>
    public string SecretKey { get; set; } = string.Empty;

    /// <summary>
    /// 签发者
    /// </summary>
    public string Issuer { get; set; } = "Making.Jwt";

    /// <summary>
    /// 受众
    /// </summary>
    public string Audience { get; set; } = "Making.Client";

    /// <summary>
    /// 访问令牌过期时间（分钟）
    /// </summary>
    public int AccessTokenExpirationMinutes { get; set; } = 60;

    /// <summary>
    /// 刷新令牌过期时间（天）
    /// </summary>
    public int RefreshTokenExpirationDays { get; set; } = 30;

    /// <summary>
    /// 是否验证签发者
    /// </summary>
    public bool ValidateIssuer { get; set; } = true;

    /// <summary>
    /// 是否验证受众
    /// </summary>
    public bool ValidateAudience { get; set; } = true;

    /// <summary>
    /// 是否验证生命周期
    /// </summary>
    public bool ValidateLifetime { get; set; } = true;

    /// <summary>
    /// 是否验证签名密钥
    /// </summary>
    public bool ValidateIssuerSigningKey { get; set; } = true;

    /// <summary>
    /// 时钟偏差（秒）
    /// </summary>
    public int ClockSkewSeconds { get; set; } = 300;

    /// <summary>
    /// 是否需要HTTPS元数据
    /// </summary>
    public bool RequireHttpsMetadata { get; set; } = true;

    /// <summary>
    /// 是否保存令牌
    /// </summary>
    public bool SaveToken { get; set; } = true;

    /// <summary>
    /// 签名算法
    /// </summary>
    public string SigningAlgorithm { get; set; } = SecurityAlgorithms.HmacSha256;

    /// <summary>
    /// 是否包含错误详情
    /// </summary>
    public bool IncludeErrorDetails { get; set; } = false;

    /// <summary>
    /// 是否启用刷新令牌
    /// </summary>
    public bool EnableRefreshToken { get; set; } = true;

    /// <summary>
    /// 刷新令牌长度
    /// </summary>
    public int RefreshTokenLength { get; set; } = 32;

    /// <summary>
    /// 是否允许多个活跃的刷新令牌
    /// </summary>
    public bool AllowMultipleActiveRefreshTokens { get; set; } = false;

    /// <summary>
    /// 最大活跃刷新令牌数量
    /// </summary>
    public int MaxActiveRefreshTokens { get; set; } = 5;

    /// <summary>
    /// 是否在刷新时撤销旧的刷新令牌
    /// </summary>
    public bool RevokeRefreshTokenOnRefresh { get; set; } = true;

    /// <summary>
    /// 是否启用JWT ID
    /// </summary>
    public bool EnableJti { get; set; } = true;

    /// <summary>
    /// 是否启用令牌黑名单
    /// </summary>
    public bool EnableTokenBlacklist { get; set; } = false;

    /// <summary>
    /// 令牌黑名单缓存过期时间（分钟）
    /// </summary>
    public int TokenBlacklistCacheExpirationMinutes { get; set; } = 60;

    /// <summary>
    /// 自定义声明前缀
    /// </summary>
    public string CustomClaimPrefix { get; set; } = "mark_";

    /// <summary>
    /// 额外的验证参数
    /// </summary>
    public Dictionary<string, object> AdditionalValidationParameters { get; set; } = new();

    /// <summary>
    /// 自定义头部声明
    /// </summary>
    public Dictionary<string, object> CustomHeaderClaims { get; set; } = new();

    /// <summary>
    /// 验证密钥是否已设置
    /// </summary>
    /// <returns></returns>
    public bool IsSecretKeySet()
    {
        return !string.IsNullOrEmpty(SecretKey);
    }

    /// <summary>
    /// 获取访问令牌过期时间
    /// </summary>
    /// <returns></returns>
    public TimeSpan GetAccessTokenExpiration()
    {
        return TimeSpan.FromMinutes(AccessTokenExpirationMinutes);
    }

    /// <summary>
    /// 获取刷新令牌过期时间
    /// </summary>
    /// <returns></returns>
    public TimeSpan GetRefreshTokenExpiration()
    {
        return TimeSpan.FromDays(RefreshTokenExpirationDays);
    }

    /// <summary>
    /// 获取时钟偏差
    /// </summary>
    /// <returns></returns>
    public TimeSpan GetClockSkew()
    {
        return TimeSpan.FromSeconds(ClockSkewSeconds);
    }

    /// <summary>
    /// 验证配置
    /// </summary>
    /// <returns></returns>
    public IEnumerable<string> Validate()
    {
        var errors = new List<string>();

        if (string.IsNullOrEmpty(SecretKey))
            errors.Add("SecretKey is required");

        if (SecretKey.Length < 16)
            errors.Add("SecretKey must be at least 16 characters long");

        if (string.IsNullOrEmpty(Issuer))
            errors.Add("Issuer is required");

        if (string.IsNullOrEmpty(Audience))
            errors.Add("Audience is required");

        if (AccessTokenExpirationMinutes <= 0)
            errors.Add("AccessTokenExpirationMinutes must be greater than 0");

        if (RefreshTokenExpirationDays <= 0)
            errors.Add("RefreshTokenExpirationDays must be greater than 0");

        if (ClockSkewSeconds < 0)
            errors.Add("ClockSkewSeconds must be greater than or equal to 0");

        if (RefreshTokenLength < 16)
            errors.Add("RefreshTokenLength must be at least 16");

        if (MaxActiveRefreshTokens <= 0)
            errors.Add("MaxActiveRefreshTokens must be greater than 0");

        return errors;
    }
}
