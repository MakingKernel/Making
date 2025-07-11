using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Mark.Jwt.Models;
using Mark.Security.Claims;
using Mark.Security.Extensions;
using Mark.Security.Users;
using Microsoft.IdentityModel.Tokens;

namespace Mark.Jwt.Helpers;

/// <summary>
/// JWT辅助工具类
/// </summary>
public static class JwtHelper
{

    /// <summary>
    /// 从CurrentUser创建JwtClaims
    /// </summary>
    /// <param name="currentUser">当前用户</param>
    /// <returns>JWT声明</returns>
    public static JwtClaims ToJwtClaims(this ICurrentUser currentUser)
    {
        return new JwtClaims
        {
            UserId = currentUser.Id,
            UserName = currentUser.UserName,
            Name = currentUser.Name,
            SurName = currentUser.SurName,
            Email = currentUser.Email,
            EmailVerified = currentUser.EmailVerified,
            PhoneNumber = currentUser.PhoneNumber,
            PhoneNumberVerified = currentUser.PhoneNumberVerified,
            TenantId = currentUser.TenantId,
            Roles = currentUser.Roles,
            SessionId = currentUser.FindSessionId(),
            ImpersonatorTenantId = currentUser.FindImpersonatorTenantId(),
            ImpersonatorUserId = currentUser.FindImpersonatorUserId(),
            ImpersonatorTenantName = currentUser.FindImpersonatorTenantName(),
            ImpersonatorUserName = currentUser.FindImpersonatorUserName()
        };
    }

    /// <summary>
    /// 解码JWT令牌（不验证签名）
    /// </summary>
    /// <param name="token">JWT令牌</param>
    /// <returns>JWT安全令牌</returns>
    public static JwtSecurityToken? DecodeToken(string token)
    {
        try
        {
            var handler = new JwtSecurityTokenHandler();
            return handler.ReadJwtToken(token);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// 从JWT令牌中提取声明
    /// </summary>
    /// <param name="token">JWT令牌</param>
    /// <returns>声明字典</returns>
    public static Dictionary<string, object> ExtractClaimsDictionary(string token)
    {
        var jwtToken = DecodeToken(token);
        if (jwtToken == null) return new Dictionary<string, object>();

        var claims = new Dictionary<string, object>();
        foreach (var claim in jwtToken.Claims)
        {
            if (claims.ContainsKey(claim.Type))
            {
                // 处理多值声明（如角色）
                if (claims[claim.Type] is List<string> list)
                {
                    list.Add(claim.Value);
                }
                else
                {
                    claims[claim.Type] = new List<string> { claims[claim.Type].ToString()!, claim.Value };
                }
            }
            else
            {
                claims[claim.Type] = claim.Value;
            }
        }

        return claims;
    }

    /// <summary>
    /// 检查JWT令牌是否过期（不验证签名）
    /// </summary>
    /// <param name="token">JWT令牌</param>
    /// <returns>是否过期</returns>
    public static bool IsTokenExpired(string token)
    {
        var jwtToken = DecodeToken(token);
        return jwtToken?.ValidTo < DateTime.UtcNow;
    }

    /// <summary>
    /// 获取JWT令牌的剩余有效时间
    /// </summary>
    /// <param name="token">JWT令牌</param>
    /// <returns>剩余时间</returns>
    public static TimeSpan? GetTokenRemainingTime(string token)
    {
        var jwtToken = DecodeToken(token);
        if (jwtToken == null) return null;

        var remaining = jwtToken.ValidTo - DateTime.UtcNow;
        return remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero;
    }

    /// <summary>
    /// 创建对称安全密钥
    /// </summary>
    /// <param name="secretKey">密钥字符串</param>
    /// <returns>对称安全密钥</returns>
    public static SymmetricSecurityKey CreateSymmetricSecurityKey(string secretKey)
    {
        return new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
    }

    /// <summary>
    /// 创建签名凭据
    /// </summary>
    /// <param name="secretKey">密钥字符串</param>
    /// <param name="algorithm">签名算法</param>
    /// <returns>签名凭据</returns>
    public static SigningCredentials CreateSigningCredentials(string secretKey, string algorithm = SecurityAlgorithms.HmacSha256)
    {
        var key = CreateSymmetricSecurityKey(secretKey);
        return new SigningCredentials(key, algorithm);
    }

    /// <summary>
    /// 验证JWT令牌格式
    /// </summary>
    /// <param name="token">JWT令牌</param>
    /// <returns>是否为有效格式</returns>
    public static bool IsValidJwtFormat(string token)
    {
        if (string.IsNullOrEmpty(token)) return false;

        var parts = token.Split('.');
        return parts.Length == 3;
    }

    /// <summary>
    /// 从声明中提取用户ID
    /// </summary>
    /// <param name="claims">声明集合</param>
    /// <returns>用户ID</returns>
    public static Guid? ExtractUserId(IEnumerable<Claim> claims)
    {
        var userIdClaim = claims.FirstOrDefault(c => c.Type == MarkClaimType.UserId);
        if (userIdClaim != null && Guid.TryParse(userIdClaim.Value, out var userId))
        {
            return userId;
        }
        return null;
    }

    /// <summary>
    /// 从声明中提取租户ID
    /// </summary>
    /// <param name="claims">声明集合</param>
    /// <returns>租户ID</returns>
    public static Guid? ExtractTenantId(IEnumerable<Claim> claims)
    {
        var tenantIdClaim = claims.FirstOrDefault(c => c.Type == MarkClaimType.TenantId);
        if (tenantIdClaim != null && Guid.TryParse(tenantIdClaim.Value, out var tenantId))
        {
            return tenantId;
        }
        return null;
    }

    /// <summary>
    /// 从声明中提取角色
    /// </summary>
    /// <param name="claims">声明集合</param>
    /// <returns>角色数组</returns>
    public static string[] ExtractRoles(IEnumerable<Claim> claims)
    {
        return claims.Where(c => c.Type == MarkClaimType.Role)
                    .Select(c => c.Value)
                    .Distinct()
                    .ToArray();
    }

    /// <summary>
    /// 创建基本的用户声明
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="userName">用户名</param>
    /// <param name="tenantId">租户ID</param>
    /// <param name="roles">角色</param>
    /// <returns>声明数组</returns>
    public static Claim[] CreateBasicUserClaims(Guid userId, string userName, Guid? tenantId = null, string[]? roles = null)
    {
        var claims = new List<Claim>
        {
            new(MarkClaimType.UserId, userId.ToString()),
            new(MarkClaimType.UserName, userName),
            new(MarkClaimType.SessionId, Guid.NewGuid().ToString())
        };

        if (tenantId.HasValue)
        {
            claims.Add(new Claim(MarkClaimType.TenantId, tenantId.Value.ToString()));
        }

        if (roles != null && roles.Length > 0)
        {
            claims.AddRange(roles.Select(role => new Claim(MarkClaimType.Role, role)));
        }

        return claims.ToArray();
    }

    /// <summary>
    /// 合并声明集合
    /// </summary>
    /// <param name="claimSets">声明集合数组</param>
    /// <returns>合并后的声明数组</returns>
    public static Claim[] MergeClaims(params IEnumerable<Claim>[] claimSets)
    {
        var allClaims = new List<Claim>();
        var seenClaims = new HashSet<string>();

        foreach (var claimSet in claimSets)
        {
            foreach (var claim in claimSet)
            {
                var key = $"{claim.Type}:{claim.Value}";
                if (!seenClaims.Contains(key))
                {
                    allClaims.Add(claim);
                    seenClaims.Add(key);
                }
            }
        }

        return allClaims.ToArray();
    }

    /// <summary>
    /// 检查声明中是否包含指定角色
    /// </summary>
    /// <param name="claims">声明集合</param>
    /// <param name="role">角色名称</param>
    /// <returns>是否包含角色</returns>
    public static bool HasRole(IEnumerable<Claim> claims, string role)
    {
        return claims.Any(c => c.Type == MarkClaimType.Role && 
                              string.Equals(c.Value, role, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// 检查声明中是否包含任意指定角色
    /// </summary>
    /// <param name="claims">声明集合</param>
    /// <param name="roles">角色名称数组</param>
    /// <returns>是否包含任意角色</returns>
    public static bool HasAnyRole(IEnumerable<Claim> claims, params string[] roles)
    {
        var userRoles = ExtractRoles(claims);
        return roles.Any(role => userRoles.Contains(role, StringComparer.OrdinalIgnoreCase));
    }
}
