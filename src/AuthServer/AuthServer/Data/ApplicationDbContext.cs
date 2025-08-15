using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using AuthServer.Models;
using Microsoft.AspNetCore.Identity;

namespace AuthServer.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser, AdminRole, string>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    // 添加新的DbSet
    public DbSet<AuditLog> AuditLogs { get; set; }
    public DbSet<Permission> Permissions { get; set; }
    public DbSet<RolePermission> RolePermissions { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        
        // 使用OpenIddict实体配置
        builder.UseOpenIddict();
        
        // 配置ApplicationUser
        builder.Entity<ApplicationUser>(entity =>
        {
            entity.Property(e => e.FirstName).HasMaxLength(100);
            entity.Property(e => e.LastName).HasMaxLength(100);
            entity.Property(e => e.Avatar).HasMaxLength(500);
            entity.Property(e => e.Department).HasMaxLength(200);
            entity.Property(e => e.Position).HasMaxLength(200);
            entity.Property(e => e.Notes).HasMaxLength(1000);
            entity.Property(e => e.LastLoginIp).HasMaxLength(45);
            
            // 添加索引
            entity.HasIndex(e => e.Email).IsUnique();
            entity.HasIndex(e => e.IsActive);
            entity.HasIndex(e => e.CreatedAt);
            entity.HasIndex(e => e.LastLoginAt);
        });

        // 配置AdminRole
        builder.Entity<AdminRole>(entity =>
        {
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.HasIndex(e => e.IsSystemRole);
        });

        // 配置AuditLog
        builder.Entity<AuditLog>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Action).HasMaxLength(100).IsRequired();
            entity.Property(e => e.ResourceType).HasMaxLength(100);
            entity.Property(e => e.ResourceId).HasMaxLength(100);
            entity.Property(e => e.Description).HasMaxLength(2000);
            entity.Property(e => e.IpAddress).HasMaxLength(45);
            entity.Property(e => e.UserAgent).HasMaxLength(500);
            entity.Property(e => e.Source).HasMaxLength(100);
            entity.Property(e => e.SessionId).HasMaxLength(100);
            entity.Property(e => e.UserName).HasMaxLength(256);
            
            // 添加索引以提高查询性能
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.Action);
            entity.HasIndex(e => e.Timestamp);
            entity.HasIndex(e => e.Result);
            entity.HasIndex(e => e.RiskLevel);
            entity.HasIndex(e => new { e.ResourceType, e.ResourceId });
            entity.HasIndex(e => e.IpAddress);
        });

        // 配置Permission
        builder.Entity<Permission>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.Group).HasMaxLength(100);
            entity.Property(e => e.Resource).HasMaxLength(100);
            entity.Property(e => e.Action).HasMaxLength(100);
            
            // 权限名称唯一
            entity.HasIndex(e => e.Name).IsUnique();
            entity.HasIndex(e => e.Group);
            entity.HasIndex(e => e.IsSystemPermission);
        });

        // 配置RolePermission (多对多关系)
        builder.Entity<RolePermission>(entity =>
        {
            entity.HasKey(e => new { e.RoleId, e.PermissionId });
            
            entity.HasOne(e => e.Role)
                .WithMany(r => r.Permissions)
                .HasForeignKey(e => e.RoleId)
                .OnDelete(DeleteBehavior.Cascade);
                
            entity.HasOne(e => e.Permission)
                .WithMany(p => p.RolePermissions)
                .HasForeignKey(e => e.PermissionId)
                .OnDelete(DeleteBehavior.Cascade);
                
            entity.HasIndex(e => e.GrantedAt);
        });
    }
}