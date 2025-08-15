using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace AuthServer.Models;

/// <summary>
/// 扩展的角色模型，支持管理员权限
/// </summary>
public class AdminRole : IdentityRole
{
    /// <summary>
    /// 角色描述
    /// </summary>
    [MaxLength(500)]
    public string? Description { get; set; }
    
    /// <summary>
    /// 是否为系统角色（不可删除）
    /// </summary>
    public bool IsSystemRole { get; set; }
    
    /// <summary>
    /// 角色权限列表
    /// </summary>
    public virtual ICollection<RolePermission> Permissions { get; set; } = new List<RolePermission>();
    
    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// 更新时间
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// 创建者ID
    /// </summary>
    public string? CreatedBy { get; set; }
    
    /// <summary>
    /// 更新者ID
    /// </summary>
    public string? UpdatedBy { get; set; }
}

/// <summary>
/// 权限实体
/// </summary>
public class Permission
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    
    /// <summary>
    /// 权限名称
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// 权限描述
    /// </summary>
    [MaxLength(500)]
    public string? Description { get; set; }
    
    /// <summary>
    /// 权限分组
    /// </summary>
    [MaxLength(100)]
    public string? Group { get; set; }
    
    /// <summary>
    /// 资源类型
    /// </summary>
    [MaxLength(100)]
    public string? Resource { get; set; }
    
    /// <summary>
    /// 操作类型
    /// </summary>
    [MaxLength(100)]
    public string? Action { get; set; }
    
    /// <summary>
    /// 是否为系统权限
    /// </summary>
    public bool IsSystemPermission { get; set; }
    
    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// 角色权限映射
    /// </summary>
    public virtual ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
}

/// <summary>
/// 角色权限映射表
/// </summary>
public class RolePermission
{
    public string RoleId { get; set; } = string.Empty;
    public string PermissionId { get; set; } = string.Empty;
    
    public virtual AdminRole Role { get; set; } = null!;
    public virtual Permission Permission { get; set; } = null!;
    
    public DateTime GrantedAt { get; set; } = DateTime.UtcNow;
    public string? GrantedBy { get; set; }
}

/// <summary>
/// 预定义的系统角色
/// </summary>
public static class SystemRoles
{
    public const string SuperAdmin = "SuperAdmin";
    public const string Admin = "Admin";
    public const string UserManager = "UserManager";
    public const string Auditor = "Auditor";
    public const string ReadOnly = "ReadOnly";
    
    /// <summary>
    /// 获取系统角色定义
    /// </summary>
    public static IEnumerable<AdminRole> GetSystemRoles()
    {
        return new[]
        {
            new AdminRole
            {
                Id = Guid.NewGuid().ToString(),
                Name = SuperAdmin,
                NormalizedName = SuperAdmin.ToUpper(),
                Description = "超级管理员，拥有所有权限",
                IsSystemRole = true
            },
            new AdminRole
            {
                Id = Guid.NewGuid().ToString(),
                Name = Admin,
                NormalizedName = Admin.ToUpper(),
                Description = "管理员，拥有大部分权限",
                IsSystemRole = true
            },
            new AdminRole
            {
                Id = Guid.NewGuid().ToString(),
                Name = UserManager,
                NormalizedName = UserManager.ToUpper(),
                Description = "用户管理员，负责用户管理",
                IsSystemRole = true
            },
            new AdminRole
            {
                Id = Guid.NewGuid().ToString(),
                Name = Auditor,
                NormalizedName = Auditor.ToUpper(),
                Description = "审计员，负责查看审计日志",
                IsSystemRole = true
            },
            new AdminRole
            {
                Id = Guid.NewGuid().ToString(),
                Name = ReadOnly,
                NormalizedName = ReadOnly.ToUpper(),
                Description = "只读用户，只能查看信息",
                IsSystemRole = true
            }
        };
    }
}