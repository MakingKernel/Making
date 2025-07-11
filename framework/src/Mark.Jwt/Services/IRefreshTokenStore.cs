namespace Mark.Jwt.Services;

/// <summary>
/// 刷新令牌存储接口
/// </summary>
public interface IRefreshTokenStore
{
    /// <summary>
    /// 存储刷新令牌
    /// </summary>
    /// <param name="refreshToken">刷新令牌</param>
    /// <param name="userId">用户ID</param>
    /// <param name="jti">JWT ID</param>
    /// <param name="expiresAt">过期时间</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns></returns>
    Task StoreRefreshTokenAsync(string refreshToken, Guid userId, string jti, DateTime expiresAt, CancellationToken cancellationToken = default);

    /// <summary>
    /// 验证刷新令牌
    /// </summary>
    /// <param name="refreshToken">刷新令牌</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>刷新令牌信息</returns>
    Task<RefreshTokenInfo?> ValidateRefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default);

    /// <summary>
    /// 撤销刷新令牌
    /// </summary>
    /// <param name="refreshToken">刷新令牌</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns></returns>
    Task RevokeRefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default);

    /// <summary>
    /// 撤销用户的所有刷新令牌
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns></returns>
    Task RevokeAllUserRefreshTokensAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 清理过期的刷新令牌
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns></returns>
    Task CleanupExpiredTokensAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取用户的活跃刷新令牌数量
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns></returns>
    Task<int> GetActiveRefreshTokenCountAsync(Guid userId, CancellationToken cancellationToken = default);
}

/// <summary>
/// 刷新令牌信息
/// </summary>
public class RefreshTokenInfo
{
    /// <summary>
    /// 刷新令牌
    /// </summary>
    public string RefreshToken { get; set; } = string.Empty;

    /// <summary>
    /// 用户ID
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// JWT ID
    /// </summary>
    public string Jti { get; set; } = string.Empty;

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// 过期时间
    /// </summary>
    public DateTime ExpiresAt { get; set; }

    /// <summary>
    /// 是否已撤销
    /// </summary>
    public bool IsRevoked { get; set; }

    /// <summary>
    /// 撤销时间
    /// </summary>
    public DateTime? RevokedAt { get; set; }

    /// <summary>
    /// 客户端ID
    /// </summary>
    public string? ClientId { get; set; }

    /// <summary>
    /// IP地址
    /// </summary>
    public string? IpAddress { get; set; }

    /// <summary>
    /// 用户代理
    /// </summary>
    public string? UserAgent { get; set; }

    /// <summary>
    /// 检查是否有效
    /// </summary>
    /// <returns></returns>
    public bool IsValid()
    {
        return !IsRevoked && DateTime.UtcNow < ExpiresAt;
    }

    /// <summary>
    /// 检查是否过期
    /// </summary>
    /// <returns></returns>
    public bool IsExpired()
    {
        return DateTime.UtcNow >= ExpiresAt;
    }
}
