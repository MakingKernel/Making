using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace AuthServer.Models;

public class ApplicationUser : IdentityUser
{
    [MaxLength(100)]
    public string? FirstName { get; set; }
    
    [MaxLength(100)]
    public string? LastName { get; set; }
    
    [MaxLength(500)]
    public string? Avatar { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;
    
    /// <summary>
    /// 最后登录时间
    /// </summary>
    public DateTime? LastLoginAt { get; set; }
    
    /// <summary>
    /// 最后登录IP
    /// </summary>
    [MaxLength(45)]
    public string? LastLoginIp { get; set; }
    
    /// <summary>
    /// 登录次数
    /// </summary>
    public int LoginCount { get; set; } = 0;
    
    /// <summary>
    /// 失败登录次数
    /// </summary>
    public int FailedLoginCount { get; set; } = 0;
    
    /// <summary>
    /// 是否为管理员
    /// </summary>
    public bool IsAdmin { get; set; } = false;
    
    /// <summary>
    /// 部门
    /// </summary>
    [MaxLength(200)]
    public string? Department { get; set; }
    
    /// <summary>
    /// 职位
    /// </summary>
    [MaxLength(200)]
    public string? Position { get; set; }
    
    /// <summary>
    /// 备注
    /// </summary>
    [MaxLength(1000)]
    public string? Notes { get; set; }
    
    /// <summary>
    /// 创建者ID
    /// </summary>
    public string? CreatedBy { get; set; }
    
    /// <summary>
    /// 更新者ID
    /// </summary>
    public string? UpdatedBy { get; set; }
    
    /// <summary>
    /// 显示名称
    /// </summary>
    public string DisplayName => $"{FirstName} {LastName}".Trim();
    
    /// <summary>
    /// 完整名称
    /// </summary>
    public string FullName => string.IsNullOrEmpty(DisplayName) ? (Email ?? UserName ?? "Unknown") : DisplayName;
}