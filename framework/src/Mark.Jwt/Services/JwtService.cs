using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Mark.Jwt.Models;
using Mark.Jwt.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Mark.Jwt.Services;

/// <summary>
/// JWT服务实现
/// </summary>
public class JwtService : IJwtService
{
    private readonly JwtOptions _options;
    private readonly IRefreshTokenStore? _refreshTokenStore;
    private readonly ILogger<JwtService> _logger;
    private readonly JwtSecurityTokenHandler _tokenHandler;
    private readonly SymmetricSecurityKey _signingKey;

    public JwtService(
        IOptions<JwtOptions> options,
        ILogger<JwtService> logger,
        IRefreshTokenStore? refreshTokenStore = null)
    {
        _options = options.Value;
        _refreshTokenStore = refreshTokenStore;
        _logger = logger;
        _tokenHandler = new JwtSecurityTokenHandler();
        
        if (string.IsNullOrEmpty(_options.SecretKey))
            throw new ArgumentException("JWT SecretKey is required", nameof(options));
        
        _signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SecretKey));
    }

    public async Task<TokenResult> GenerateTokenAsync(JwtClaims claims, CancellationToken cancellationToken = default)
    {
        return await GenerateTokenAsync(claims.ToClaims(), cancellationToken);
    }

    public async Task<TokenResult> GenerateTokenAsync(IEnumerable<Claim> claims, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var expires = now.Add(_options.GetAccessTokenExpiration());
        var jti = _options.EnableJti ? Guid.NewGuid().ToString() : null;

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = expires,
            Issuer = _options.Issuer,
            Audience = _options.Audience,
            SigningCredentials = new SigningCredentials(_signingKey, _options.SigningAlgorithm),
            NotBefore = now,
            IssuedAt = now
        };

        // 添加JWT ID
        if (!string.IsNullOrEmpty(jti))
        {
            tokenDescriptor.Subject.AddClaim(new Claim(JwtRegisteredClaimNames.Jti, jti));
        }

        // 添加自定义头部声明
        if (_options.CustomHeaderClaims.Count > 0)
        {
            tokenDescriptor.AdditionalHeaderClaims = _options.CustomHeaderClaims;
        }

        var token = _tokenHandler.CreateToken(tokenDescriptor);
        var accessToken = _tokenHandler.WriteToken(token);

        string? refreshToken = null;
        if (_options.EnableRefreshToken)
        {
            var userIdClaim = claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);
            if (userIdClaim != null && Guid.TryParse(userIdClaim.Value, out var userId))
            {
                refreshToken = await GenerateRefreshTokenAsync(userId, jti ?? Guid.NewGuid().ToString(), cancellationToken);
            }
        }

        var expiresIn = (int)_options.GetAccessTokenExpiration().TotalSeconds;
        var jwtClaims = JwtClaims.FromClaims(claims);

        return TokenResult.Success(accessToken, refreshToken, expiresIn, jwtClaims, jti);
    }

    public async Task<JwtValidationResult> ValidateTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        try
        {
            var validationParameters = CreateTokenValidationParameters();
            
            var principal = _tokenHandler.ValidateToken(token, validationParameters, out var validatedToken);
            
            if (validatedToken is not JwtSecurityToken jwtToken)
            {
                return JwtValidationResult.Failure("Invalid token format");
            }

            var claims = JwtClaims.FromClaims(principal.Claims);
            
            return JwtValidationResult.Success(
                principal, 
                claims,
                jwtToken.ValidTo,
                jwtToken.IssuedAt,
                jwtToken.ValidFrom,
                jwtToken.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Jti)?.Value,
                jwtToken.Issuer,
                jwtToken.Audiences.FirstOrDefault()
            );
        }
        catch (SecurityTokenExpiredException ex)
        {
            _logger.LogWarning("Token expired: {Message}", ex.Message);
            return JwtValidationResult.Failure("Token has expired", "token_expired", ex);
        }
        catch (SecurityTokenInvalidSignatureException ex)
        {
            _logger.LogWarning("Invalid token signature: {Message}", ex.Message);
            return JwtValidationResult.Failure("Invalid token signature", "invalid_signature", ex);
        }
        catch (SecurityTokenValidationException ex)
        {
            _logger.LogWarning("Token validation failed: {Message}", ex.Message);
            return JwtValidationResult.Failure("Token validation failed", "validation_failed", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during token validation");
            return JwtValidationResult.Failure("Token validation error", "validation_error", ex);
        }
    }

    public async Task<TokenResult?> RefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        if (_refreshTokenStore == null)
        {
            _logger.LogWarning("Refresh token store is not configured");
            return null;
        }

        var refreshTokenInfo = await _refreshTokenStore.ValidateRefreshTokenAsync(refreshToken, cancellationToken);
        if (refreshTokenInfo == null || !refreshTokenInfo.IsValid())
        {
            _logger.LogWarning("Invalid or expired refresh token");
            return null;
        }

        // 撤销旧的刷新令牌
        if (_options.RevokeRefreshTokenOnRefresh)
        {
            await _refreshTokenStore.RevokeRefreshTokenAsync(refreshToken, cancellationToken);
        }

        // 创建新的声明（这里需要从用户存储中获取最新的用户信息）
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, refreshTokenInfo.UserId.ToString()),
            new(JwtRegisteredClaimNames.Jti, refreshTokenInfo.Jti)
        };

        // 这里应该从用户服务获取最新的用户信息和声明
        // 为了简化，我们使用基本的声明
        
        return await GenerateTokenAsync(claims, cancellationToken);
    }

    public async Task RevokeRefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        if (_refreshTokenStore != null)
        {
            await _refreshTokenStore.RevokeRefreshTokenAsync(refreshToken, cancellationToken);
        }
    }

    public async Task RevokeAllUserTokensAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        if (_refreshTokenStore != null)
        {
            await _refreshTokenStore.RevokeAllUserRefreshTokensAsync(userId, cancellationToken);
        }
    }

    public IEnumerable<Claim>? ExtractClaims(string token)
    {
        try
        {
            var jwtToken = _tokenHandler.ReadJwtToken(token);
            return jwtToken.Claims;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to extract claims from token");
            return null;
        }
    }

    public JwtClaims? ExtractJwtClaims(string token)
    {
        var claims = ExtractClaims(token);
        return claims != null ? JwtClaims.FromClaims(claims) : null;
    }

    public bool IsTokenValid(string token)
    {
        try
        {
            var validationParameters = CreateTokenValidationParameters();
            validationParameters.ValidateLifetime = false; // 不验证过期时间
            
            _tokenHandler.ValidateToken(token, validationParameters, out _);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public DateTime? GetTokenExpiration(string token)
    {
        try
        {
            var jwtToken = _tokenHandler.ReadJwtToken(token);
            return jwtToken.ValidTo;
        }
        catch
        {
            return null;
        }
    }

    public DateTime? GetTokenIssuedAt(string token)
    {
        try
        {
            var jwtToken = _tokenHandler.ReadJwtToken(token);
            return jwtToken.IssuedAt;
        }
        catch
        {
            return null;
        }
    }

    public string? GetTokenJti(string token)
    {
        try
        {
            var jwtToken = _tokenHandler.ReadJwtToken(token);
            return jwtToken.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Jti)?.Value;
        }
        catch
        {
            return null;
        }
    }

    public async Task<bool> IsRefreshTokenValidAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        if (_refreshTokenStore == null) return false;
        
        var refreshTokenInfo = await _refreshTokenStore.ValidateRefreshTokenAsync(refreshToken, cancellationToken);
        return refreshTokenInfo?.IsValid() == true;
    }

    public async Task<string> GenerateRefreshTokenAsync(Guid userId, string jti, CancellationToken cancellationToken = default)
    {
        var refreshToken = GenerateSecureRandomString(_options.RefreshTokenLength);
        
        if (_refreshTokenStore != null)
        {
            var expiresAt = DateTime.UtcNow.Add(_options.GetRefreshTokenExpiration());
            await _refreshTokenStore.StoreRefreshTokenAsync(refreshToken, userId, jti, expiresAt, cancellationToken);
        }
        
        return refreshToken;
    }

    public ClaimsPrincipal CreatePrincipal(IEnumerable<Claim> claims, string authenticationType = "JWT")
    {
        var identity = new ClaimsIdentity(claims, authenticationType);
        return new ClaimsPrincipal(identity);
    }

    public ClaimsPrincipal CreatePrincipal(JwtClaims jwtClaims, string authenticationType = "JWT")
    {
        return CreatePrincipal(jwtClaims.ToClaims(), authenticationType);
    }

    private TokenValidationParameters CreateTokenValidationParameters()
    {
        var parameters = new TokenValidationParameters
        {
            ValidateIssuer = _options.ValidateIssuer,
            ValidateAudience = _options.ValidateAudience,
            ValidateLifetime = _options.ValidateLifetime,
            ValidateIssuerSigningKey = _options.ValidateIssuerSigningKey,
            ValidIssuer = _options.Issuer,
            ValidAudience = _options.Audience,
            IssuerSigningKey = _signingKey,
            ClockSkew = _options.GetClockSkew(),
            RequireExpirationTime = true
        };

        // 应用额外的验证参数
        foreach (var parameter in _options.AdditionalValidationParameters)
        {
            var property = typeof(TokenValidationParameters).GetProperty(parameter.Key);
            if (property != null && property.CanWrite)
            {
                property.SetValue(parameters, parameter.Value);
            }
        }

        return parameters;
    }

    private static string GenerateSecureRandomString(int length)
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
        var random = new byte[length];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(random);
        }
        
        var result = new StringBuilder(length);
        foreach (var b in random)
        {
            result.Append(chars[b % chars.Length]);
        }
        
        return result.ToString();
    }
}
