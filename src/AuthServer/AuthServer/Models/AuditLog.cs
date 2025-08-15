using System.ComponentModel.DataAnnotations;

namespace AuthServer.Models;

/// <summary>
/// 审计日志实体
/// </summary>
public class AuditLog
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    
    /// <summary>
    /// 操作的用户ID
    /// </summary>
    public string? UserId { get; set; }
    
    /// <summary>
    /// 用户名（冗余字段，便于查询）
    /// </summary>
    public string? UserName { get; set; }
    
    /// <summary>
    /// 操作类型（登录、登出、创建用户、修改权限等）
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string Action { get; set; } = string.Empty;
    
    /// <summary>
    /// 操作的资源/实体类型
    /// </summary>
    [MaxLength(100)]
    public string? ResourceType { get; set; }
    
    /// <summary>
    /// 操作的资源ID
    /// </summary>
    [MaxLength(100)]
    public string? ResourceId { get; set; }
    
    /// <summary>
    /// 详细描述
    /// </summary>
    [MaxLength(2000)]
    public string? Description { get; set; }
    
    /// <summary>
    /// IP地址
    /// </summary>
    [MaxLength(45)]
    public string? IpAddress { get; set; }
    
    /// <summary>
    /// 用户代理
    /// </summary>
    [MaxLength(500)]
    public string? UserAgent { get; set; }
    
    /// <summary>
    /// 操作结果
    /// </summary>
    public AuditResult Result { get; set; }
    
    /// <summary>
    /// 操作时间
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// 额外的上下文数据（JSON格式）
    /// </summary>
    public string? AdditionalData { get; set; }
    
    /// <summary>
    /// 风险等级
    /// </summary>
    public RiskLevel RiskLevel { get; set; } = RiskLevel.Low;
    
    /// <summary>
    /// 操作来源
    /// </summary>
    [MaxLength(100)]
    public string? Source { get; set; }
    
    /// <summary>
    /// 关联的会话ID
    /// </summary>
    [MaxLength(100)]
    public string? SessionId { get; set; }
}

/// <summary>
/// 审计结果枚举
/// </summary>
public enum AuditResult
{
    Success = 0,
    Failed = 1,
    Warning = 2,
    Error = 3
}

/// <summary>
/// 风险等级枚举
/// </summary>
public enum RiskLevel
{
    Low = 0,
    Medium = 1,
    High = 2,
    Critical = 3
}

/// <summary>
/// 常见的审计操作类型
/// </summary>
public static class AuditActions
{
    // 认证相关
    public const string LoginSuccess = "LOGIN_SUCCESS";
    public const string LoginFailed = "LOGIN_FAILED";
    public const string Logout = "LOGOUT";
    public const string PasswordChanged = "PASSWORD_CHANGED";
    public const string PasswordReset = "PASSWORD_RESET";
    public const string TwoFactorEnabled = "TWO_FACTOR_ENABLED";
    public const string TwoFactorDisabled = "TWO_FACTOR_DISABLED";
    
    // 用户管理
    public const string UserCreated = "USER_CREATED";
    public const string UserUpdated = "USER_UPDATED";
    public const string UserDeleted = "USER_DELETED";
    public const string UserLocked = "USER_LOCKED";
    public const string UserUnlocked = "USER_UNLOCKED";
    public const string UserActivated = "USER_ACTIVATED";
    public const string UserDeactivated = "USER_DEACTIVATED";
    
    // 角色权限
    public const string RoleAssigned = "ROLE_ASSIGNED";
    public const string RoleRemoved = "ROLE_REMOVED";
    public const string RoleCreated = "ROLE_CREATED";
    public const string RoleUpdated = "ROLE_UPDATED";
    public const string RoleDeleted = "ROLE_DELETED";
    
    // 客户端管理
    public const string ClientCreated = "CLIENT_CREATED";
    public const string ClientUpdated = "CLIENT_UPDATED";
    public const string ClientDeleted = "CLIENT_DELETED";
    
    // 系统操作
    public const string SystemConfigChanged = "SYSTEM_CONFIG_CHANGED";
    public const string DataExported = "DATA_EXPORTED";
    public const string DataImported = "DATA_IMPORTED";
    
    // 安全事件
    public const string SecurityViolation = "SECURITY_VIOLATION";
    public const string SuspiciousActivity = "SUSPICIOUS_ACTIVITY";
    public const string AccountLockout = "ACCOUNT_LOCKOUT";
}