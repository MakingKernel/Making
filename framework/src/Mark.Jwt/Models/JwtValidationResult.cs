using System.Security.Claims;

namespace Mark.Jwt.Models;

/// <summary>
/// JWT验证结果
/// </summary>
public class JwtValidationResult
{
    /// <summary>
    /// 是否验证成功
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// 验证后的用户主体
    /// </summary>
    public ClaimsPrincipal? Principal { get; set; }

    /// <summary>
    /// JWT声明信息
    /// </summary>
    public JwtClaims? Claims { get; set; }

    /// <summary>
    /// 令牌过期时间
    /// </summary>
    public DateTime? ExpiresAt { get; set; }

    /// <summary>
    /// 令牌签发时间
    /// </summary>
    public DateTime? IssuedAt { get; set; }

    /// <summary>
    /// 令牌生效时间
    /// </summary>
    public DateTime? NotBefore { get; set; }

    /// <summary>
    /// JWT ID
    /// </summary>
    public string? Jti { get; set; }

    /// <summary>
    /// 发行者
    /// </summary>
    public string? Issuer { get; set; }

    /// <summary>
    /// 受众
    /// </summary>
    public string? Audience { get; set; }

    /// <summary>
    /// 错误信息
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// 错误代码
    /// </summary>
    public string? ErrorCode { get; set; }

    /// <summary>
    /// 验证异常
    /// </summary>
    public Exception? Exception { get; set; }

    /// <summary>
    /// 创建成功的验证结果
    /// </summary>
    /// <param name="principal">用户主体</param>
    /// <param name="claims">JWT声明</param>
    /// <param name="expiresAt">过期时间</param>
    /// <param name="issuedAt">签发时间</param>
    /// <param name="notBefore">生效时间</param>
    /// <param name="jti">JWT ID</param>
    /// <param name="issuer">发行者</param>
    /// <param name="audience">受众</param>
    /// <returns></returns>
    public static JwtValidationResult Success(ClaimsPrincipal principal, JwtClaims claims, 
        DateTime? expiresAt = null, DateTime? issuedAt = null, DateTime? notBefore = null,
        string? jti = null, string? issuer = null, string? audience = null)
    {
        return new JwtValidationResult
        {
            IsValid = true,
            Principal = principal,
            Claims = claims,
            ExpiresAt = expiresAt,
            IssuedAt = issuedAt,
            NotBefore = notBefore,
            Jti = jti,
            Issuer = issuer,
            Audience = audience
        };
    }

    /// <summary>
    /// 创建失败的验证结果
    /// </summary>
    /// <param name="errorMessage">错误信息</param>
    /// <param name="errorCode">错误代码</param>
    /// <param name="exception">异常</param>
    /// <returns></returns>
    public static JwtValidationResult Failure(string errorMessage, string? errorCode = null, Exception? exception = null)
    {
        return new JwtValidationResult
        {
            IsValid = false,
            ErrorMessage = errorMessage,
            ErrorCode = errorCode,
            Exception = exception
        };
    }

    /// <summary>
    /// 检查令牌是否即将过期
    /// </summary>
    /// <param name="thresholdMinutes">阈值分钟数</param>
    /// <returns></returns>
    public bool IsExpiringSoon(int thresholdMinutes = 5)
    {
        if (!IsValid || !ExpiresAt.HasValue)
            return false;

        var threshold = DateTime.UtcNow.AddMinutes(thresholdMinutes);
        return ExpiresAt.Value <= threshold;
    }

    /// <summary>
    /// 获取剩余有效时间
    /// </summary>
    /// <returns></returns>
    public TimeSpan? GetRemainingTime()
    {
        if (!IsValid || !ExpiresAt.HasValue)
            return null;

        var remaining = ExpiresAt.Value - DateTime.UtcNow;
        return remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero;
    }
}
