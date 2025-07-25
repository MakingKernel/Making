namespace Making.Jwt.Models;

/// <summary>
/// JWT令牌结果
/// </summary>
public class TokenResult
{
    /// <summary>
    /// 访问令牌
    /// </summary>
    public string AccessToken { get; set; } = string.Empty;

    /// <summary>
    /// 刷新令牌
    /// </summary>
    public string? RefreshToken { get; set; }

    /// <summary>
    /// 令牌类型（通常为"Bearer"）
    /// </summary>
    public string TokenType { get; set; } = "Bearer";

    /// <summary>
    /// 过期时间（秒）
    /// </summary>
    public int ExpiresIn { get; set; }

    /// <summary>
    /// 过期时间戳（UTC）
    /// </summary>
    public DateTime ExpiresAt { get; set; }

    /// <summary>
    /// 签发时间戳（UTC）
    /// </summary>
    public DateTime IssuedAt { get; set; }

    /// <summary>
    /// JWT ID
    /// </summary>
    public string? Jti { get; set; }

    /// <summary>
    /// 作用域
    /// </summary>
    public string? Scope { get; set; }

    /// <summary>
    /// 用户信息
    /// </summary>
    public JwtClaims? Claims { get; set; }

    /// <summary>
    /// 创建成功的令牌结果
    /// </summary>
    /// <param name="accessToken">访问令牌</param>
    /// <param name="refreshToken">刷新令牌</param>
    /// <param name="expiresIn">过期时间（秒）</param>
    /// <param name="claims">用户声明</param>
    /// <param name="jti">JWT ID</param>
    /// <returns></returns>
    public static TokenResult Success(string accessToken, string? refreshToken, int expiresIn, 
        JwtClaims? claims = null, string? jti = null)
    {
        var now = DateTime.UtcNow;
        return new TokenResult
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresIn = expiresIn,
            ExpiresAt = now.AddSeconds(expiresIn),
            IssuedAt = now,
            Claims = claims,
            Jti = jti
        };
    }

    /// <summary>
    /// 检查令牌是否即将过期
    /// </summary>
    /// <param name="thresholdMinutes">阈值分钟数</param>
    /// <returns></returns>
    public bool IsExpiringSoon(int thresholdMinutes = 5)
    {
        var threshold = DateTime.UtcNow.AddMinutes(thresholdMinutes);
        return ExpiresAt <= threshold;
    }

    /// <summary>
    /// 获取剩余有效时间
    /// </summary>
    /// <returns></returns>
    public TimeSpan GetRemainingTime()
    {
        var remaining = ExpiresAt - DateTime.UtcNow;
        return remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero;
    }

    /// <summary>
    /// 检查令牌是否已过期
    /// </summary>
    /// <returns></returns>
    public bool IsExpired()
    {
        return DateTime.UtcNow >= ExpiresAt;
    }
}
