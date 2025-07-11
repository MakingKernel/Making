using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace Mark.Jwt.Services;

/// <summary>
/// 内存中的刷新令牌存储实现
/// </summary>
public class InMemoryRefreshTokenStore : IRefreshTokenStore
{
    private readonly ConcurrentDictionary<string, RefreshTokenInfo> _tokens = new();
    private readonly ILogger<InMemoryRefreshTokenStore> _logger;

    public InMemoryRefreshTokenStore(ILogger<InMemoryRefreshTokenStore> logger)
    {
        _logger = logger;
    }

    public Task StoreRefreshTokenAsync(string refreshToken, Guid userId, string jti, DateTime expiresAt, CancellationToken cancellationToken = default)
    {
        var tokenInfo = new RefreshTokenInfo
        {
            RefreshToken = refreshToken,
            UserId = userId,
            Jti = jti,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = expiresAt,
            IsRevoked = false
        };

        _tokens.TryAdd(refreshToken, tokenInfo);
        
        _logger.LogDebug("Stored refresh token for user {UserId} with JTI {Jti}", userId, jti);
        
        return Task.CompletedTask;
    }

    public Task<RefreshTokenInfo?> ValidateRefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        if (_tokens.TryGetValue(refreshToken, out var tokenInfo))
        {
            _logger.LogDebug("Found refresh token for user {UserId}, valid: {IsValid}", tokenInfo.UserId, tokenInfo.IsValid());
            return Task.FromResult<RefreshTokenInfo?>(tokenInfo);
        }

        _logger.LogDebug("Refresh token not found");
        return Task.FromResult<RefreshTokenInfo?>(null);
    }

    public Task RevokeRefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        if (_tokens.TryGetValue(refreshToken, out var tokenInfo))
        {
            tokenInfo.IsRevoked = true;
            tokenInfo.RevokedAt = DateTime.UtcNow;
            
            _logger.LogDebug("Revoked refresh token for user {UserId}", tokenInfo.UserId);
        }
        else
        {
            _logger.LogDebug("Attempted to revoke non-existent refresh token");
        }

        return Task.CompletedTask;
    }

    public Task RevokeAllUserRefreshTokensAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var userTokens = _tokens.Values.Where(t => t.UserId == userId && !t.IsRevoked).ToList();
        
        foreach (var token in userTokens)
        {
            token.IsRevoked = true;
            token.RevokedAt = DateTime.UtcNow;
        }

        _logger.LogDebug("Revoked {Count} refresh tokens for user {UserId}", userTokens.Count, userId);
        
        return Task.CompletedTask;
    }

    public Task CleanupExpiredTokensAsync(CancellationToken cancellationToken = default)
    {
        var expiredTokens = _tokens.Where(kvp => kvp.Value.IsExpired()).Select(kvp => kvp.Key).ToList();
        
        foreach (var expiredToken in expiredTokens)
        {
            _tokens.TryRemove(expiredToken, out _);
        }

        if (expiredTokens.Count > 0)
        {
            _logger.LogDebug("Cleaned up {Count} expired refresh tokens", expiredTokens.Count);
        }

        return Task.CompletedTask;
    }

    public Task<int> GetActiveRefreshTokenCountAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var count = _tokens.Values.Count(t => t.UserId == userId && t.IsValid());
        return Task.FromResult(count);
    }
}
