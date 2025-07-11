using System.Security.Claims;
using Mark.Security.Claims;

namespace Mark.Jwt.Models;

/// <summary>
/// JWT声明信息
/// </summary>
public class JwtClaims
{
    /// <summary>
    /// 用户ID
    /// </summary>
    public Guid? UserId { get; set; }

    /// <summary>
    /// 用户名
    /// </summary>
    public string? UserName { get; set; }

    /// <summary>
    /// 姓名
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// 姓氏
    /// </summary>
    public string? SurName { get; set; }

    /// <summary>
    /// 邮箱
    /// </summary>
    public string? Email { get; set; }

    /// <summary>
    /// 邮箱是否验证
    /// </summary>
    public bool EmailVerified { get; set; }

    /// <summary>
    /// 手机号
    /// </summary>
    public string? PhoneNumber { get; set; }

    /// <summary>
    /// 手机号是否验证
    /// </summary>
    public bool PhoneNumberVerified { get; set; }

    /// <summary>
    /// 租户ID
    /// </summary>
    public Guid? TenantId { get; set; }

    /// <summary>
    /// 版本ID
    /// </summary>
    public Guid? EditionId { get; set; }

    /// <summary>
    /// 客户端ID
    /// </summary>
    public string? ClientId { get; set; }

    /// <summary>
    /// 角色列表
    /// </summary>
    public string[]? Roles { get; set; }

    /// <summary>
    /// 会话ID
    /// </summary>
    public string? SessionId { get; set; }

    /// <summary>
    /// 头像
    /// </summary>
    public string? Picture { get; set; }

    /// <summary>
    /// 记住我
    /// </summary>
    public bool RememberMe { get; set; }

    /// <summary>
    /// 模拟者租户ID
    /// </summary>
    public Guid? ImpersonatorTenantId { get; set; }

    /// <summary>
    /// 模拟者用户ID
    /// </summary>
    public Guid? ImpersonatorUserId { get; set; }

    /// <summary>
    /// 模拟者租户名称
    /// </summary>
    public string? ImpersonatorTenantName { get; set; }

    /// <summary>
    /// 模拟者用户名
    /// </summary>
    public string? ImpersonatorUserName { get; set; }

    /// <summary>
    /// 自定义声明
    /// </summary>
    public Dictionary<string, object>? CustomClaims { get; set; }

    /// <summary>
    /// 转换为Claims数组
    /// </summary>
    /// <returns></returns>
    public Claim[] ToClaims()
    {
        var claims = new List<Claim>();

        if (UserId.HasValue)
            claims.Add(new Claim(MarkClaimType.UserId, UserId.Value.ToString()));

        if (!string.IsNullOrEmpty(UserName))
            claims.Add(new Claim(MarkClaimType.UserName, UserName));

        if (!string.IsNullOrEmpty(Name))
            claims.Add(new Claim(MarkClaimType.Name, Name));

        if (!string.IsNullOrEmpty(SurName))
            claims.Add(new Claim(MarkClaimType.SurName, SurName));

        if (!string.IsNullOrEmpty(Email))
            claims.Add(new Claim(MarkClaimType.Email, Email));

        claims.Add(new Claim(MarkClaimType.EmailVerified, EmailVerified.ToString().ToLower()));

        if (!string.IsNullOrEmpty(PhoneNumber))
            claims.Add(new Claim(MarkClaimType.PhoneNumber, PhoneNumber));

        claims.Add(new Claim(MarkClaimType.PhoneNumberVerified, PhoneNumberVerified.ToString().ToLower()));

        if (TenantId.HasValue)
            claims.Add(new Claim(MarkClaimType.TenantId, TenantId.Value.ToString()));

        if (EditionId.HasValue)
            claims.Add(new Claim(MarkClaimType.EditionId, EditionId.Value.ToString()));

        if (!string.IsNullOrEmpty(ClientId))
            claims.Add(new Claim(MarkClaimType.ClientId, ClientId));

        if (Roles != null && Roles.Length > 0)
        {
            foreach (var role in Roles)
            {
                claims.Add(new Claim(MarkClaimType.Role, role));
            }
        }

        if (!string.IsNullOrEmpty(SessionId))
            claims.Add(new Claim(MarkClaimType.SessionId, SessionId));

        if (!string.IsNullOrEmpty(Picture))
            claims.Add(new Claim(MarkClaimType.Picture, Picture));

        claims.Add(new Claim(MarkClaimType.RememberMe, RememberMe.ToString().ToLower()));

        if (ImpersonatorTenantId.HasValue)
            claims.Add(new Claim(MarkClaimType.ImpersonatorTenantId, ImpersonatorTenantId.Value.ToString()));

        if (ImpersonatorUserId.HasValue)
            claims.Add(new Claim(MarkClaimType.ImpersonatorUserId, ImpersonatorUserId.Value.ToString()));

        if (!string.IsNullOrEmpty(ImpersonatorTenantName))
            claims.Add(new Claim(MarkClaimType.ImpersonatorTenantName, ImpersonatorTenantName));

        if (!string.IsNullOrEmpty(ImpersonatorUserName))
            claims.Add(new Claim(MarkClaimType.ImpersonatorUserName, ImpersonatorUserName));

        // 添加自定义声明
        if (CustomClaims != null)
        {
            foreach (var customClaim in CustomClaims)
            {
                claims.Add(new Claim(customClaim.Key, customClaim.Value?.ToString() ?? string.Empty));
            }
        }

        return claims.ToArray();
    }

    /// <summary>
    /// 从Claims创建JwtClaims
    /// </summary>
    /// <param name="claims">声明集合</param>
    /// <returns></returns>
    public static JwtClaims FromClaims(IEnumerable<Claim> claims)
    {
        var claimsList = claims.ToList();
        var jwtClaims = new JwtClaims();

        var userIdClaim = claimsList.FirstOrDefault(c => c.Type == MarkClaimType.UserId);
        if (userIdClaim != null && Guid.TryParse(userIdClaim.Value, out var userId))
            jwtClaims.UserId = userId;

        jwtClaims.UserName = claimsList.FirstOrDefault(c => c.Type == MarkClaimType.UserName)?.Value;
        jwtClaims.Name = claimsList.FirstOrDefault(c => c.Type == MarkClaimType.Name)?.Value;
        jwtClaims.SurName = claimsList.FirstOrDefault(c => c.Type == MarkClaimType.SurName)?.Value;
        jwtClaims.Email = claimsList.FirstOrDefault(c => c.Type == MarkClaimType.Email)?.Value;

        var emailVerifiedClaim = claimsList.FirstOrDefault(c => c.Type == MarkClaimType.EmailVerified);
        if (emailVerifiedClaim != null && bool.TryParse(emailVerifiedClaim.Value, out var emailVerified))
            jwtClaims.EmailVerified = emailVerified;

        jwtClaims.PhoneNumber = claimsList.FirstOrDefault(c => c.Type == MarkClaimType.PhoneNumber)?.Value;

        var phoneVerifiedClaim = claimsList.FirstOrDefault(c => c.Type == MarkClaimType.PhoneNumberVerified);
        if (phoneVerifiedClaim != null && bool.TryParse(phoneVerifiedClaim.Value, out var phoneVerified))
            jwtClaims.PhoneNumberVerified = phoneVerified;

        var tenantIdClaim = claimsList.FirstOrDefault(c => c.Type == MarkClaimType.TenantId);
        if (tenantIdClaim != null && Guid.TryParse(tenantIdClaim.Value, out var tenantId))
            jwtClaims.TenantId = tenantId;

        var editionIdClaim = claimsList.FirstOrDefault(c => c.Type == MarkClaimType.EditionId);
        if (editionIdClaim != null && Guid.TryParse(editionIdClaim.Value, out var editionId))
            jwtClaims.EditionId = editionId;

        jwtClaims.ClientId = claimsList.FirstOrDefault(c => c.Type == MarkClaimType.ClientId)?.Value;

        var roleClaims = claimsList.Where(c => c.Type == MarkClaimType.Role).Select(c => c.Value).ToArray();
        if (roleClaims.Length > 0)
            jwtClaims.Roles = roleClaims;

        jwtClaims.SessionId = claimsList.FirstOrDefault(c => c.Type == MarkClaimType.SessionId)?.Value;
        jwtClaims.Picture = claimsList.FirstOrDefault(c => c.Type == MarkClaimType.Picture)?.Value;

        var rememberMeClaim = claimsList.FirstOrDefault(c => c.Type == MarkClaimType.RememberMe);
        if (rememberMeClaim != null && bool.TryParse(rememberMeClaim.Value, out var rememberMe))
            jwtClaims.RememberMe = rememberMe;

        var impersonatorTenantIdClaim = claimsList.FirstOrDefault(c => c.Type == MarkClaimType.ImpersonatorTenantId);
        if (impersonatorTenantIdClaim != null && Guid.TryParse(impersonatorTenantIdClaim.Value, out var impersonatorTenantId))
            jwtClaims.ImpersonatorTenantId = impersonatorTenantId;

        var impersonatorUserIdClaim = claimsList.FirstOrDefault(c => c.Type == MarkClaimType.ImpersonatorUserId);
        if (impersonatorUserIdClaim != null && Guid.TryParse(impersonatorUserIdClaim.Value, out var impersonatorUserId))
            jwtClaims.ImpersonatorUserId = impersonatorUserId;

        jwtClaims.ImpersonatorTenantName = claimsList.FirstOrDefault(c => c.Type == MarkClaimType.ImpersonatorTenantName)?.Value;
        jwtClaims.ImpersonatorUserName = claimsList.FirstOrDefault(c => c.Type == MarkClaimType.ImpersonatorUserName)?.Value;

        return jwtClaims;
    }

    /// <summary>
    /// 创建空的JwtClaims
    /// </summary>
    /// <returns></returns>
    public static JwtClaims Empty() => new();

    /// <summary>
    /// 创建用于用户的JwtClaims
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="userName">用户名</param>
    /// <param name="tenantId">租户ID</param>
    /// <returns></returns>
    public static JwtClaims ForUser(Guid userId, string userName, Guid? tenantId = null)
    {
        return new JwtClaims
        {
            UserId = userId,
            UserName = userName,
            TenantId = tenantId,
            SessionId = Guid.NewGuid().ToString()
        };
    }
}
