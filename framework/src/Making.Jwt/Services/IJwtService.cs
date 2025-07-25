using Making.Jwt.Models;
using System.Security.Claims;

namespace Making.Jwt.Services;

/// <summary>
/// JWT服务接口
/// </summary>
public interface IJwtService
{
    /// <summary>
    /// 生成JWT令牌
    /// </summary>
    /// <param name="claims">用户声明</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>令牌结果</returns>
    Task<TokenResult> GenerateTokenAsync(JwtClaims claims, CancellationToken cancellationToken = default);

    /// <summary>
    /// 生成JWT令牌（从Claims）
    /// </summary>
    /// <param name="claims">声明集合</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>令牌结果</returns>
    Task<TokenResult> GenerateTokenAsync(IEnumerable<Claim> claims, CancellationToken cancellationToken = default);

    /// <summary>
    /// 验证JWT令牌
    /// </summary>
    /// <param name="token">JWT令牌</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>验证结果</returns>
    Task<JwtValidationResult> ValidateTokenAsync(string token, CancellationToken cancellationToken = default);

    /// <summary>
    /// 刷新JWT令牌
    /// </summary>
    /// <param name="refreshToken">刷新令牌</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>新的令牌结果</returns>
    Task<TokenResult?> RefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default);

    /// <summary>
    /// 撤销刷新令牌
    /// </summary>
    /// <param name="refreshToken">刷新令牌</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns></returns>
    Task RevokeRefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default);

    /// <summary>
    /// 撤销用户的所有令牌
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns></returns>
    Task RevokeAllUserTokensAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 从令牌中提取声明
    /// </summary>
    /// <param name="token">JWT令牌</param>
    /// <returns>声明集合</returns>
    IEnumerable<Claim>? ExtractClaims(string token);

    /// <summary>
    /// 从令牌中提取JwtClaims
    /// </summary>
    /// <param name="token">JWT令牌</param>
    /// <returns>JWT声明</returns>
    JwtClaims? ExtractJwtClaims(string token);

    /// <summary>
    /// 检查令牌是否有效（不验证过期时间）
    /// </summary>
    /// <param name="token">JWT令牌</param>
    /// <returns>是否有效</returns>
    bool IsTokenValid(string token);

    /// <summary>
    /// 获取令牌过期时间
    /// </summary>
    /// <param name="token">JWT令牌</param>
    /// <returns>过期时间</returns>
    DateTime? GetTokenExpiration(string token);

    /// <summary>
    /// 获取令牌签发时间
    /// </summary>
    /// <param name="token">JWT令牌</param>
    /// <returns>签发时间</returns>
    DateTime? GetTokenIssuedAt(string token);

    /// <summary>
    /// 获取令牌的JWT ID
    /// </summary>
    /// <param name="token">JWT令牌</param>
    /// <returns>JWT ID</returns>
    string? GetTokenJti(string token);

    /// <summary>
    /// 检查刷新令牌是否有效
    /// </summary>
    /// <param name="refreshToken">刷新令牌</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否有效</returns>
    Task<bool> IsRefreshTokenValidAsync(string refreshToken, CancellationToken cancellationToken = default);

    /// <summary>
    /// 生成刷新令牌
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="jti">JWT ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>刷新令牌</returns>
    Task<string> GenerateRefreshTokenAsync(Guid userId, string jti, CancellationToken cancellationToken = default);

    /// <summary>
    /// 创建ClaimsPrincipal
    /// </summary>
    /// <param name="claims">声明集合</param>
    /// <param name="authenticationType">认证类型</param>
    /// <returns>用户主体</returns>
    ClaimsPrincipal CreatePrincipal(IEnumerable<Claim> claims, string authenticationType = "JWT");

    /// <summary>
    /// 创建ClaimsPrincipal
    /// </summary>
    /// <param name="jwtClaims">JWT声明</param>
    /// <param name="authenticationType">认证类型</param>
    /// <returns>用户主体</returns>
    ClaimsPrincipal CreatePrincipal(JwtClaims jwtClaims, string authenticationType = "JWT");
}
