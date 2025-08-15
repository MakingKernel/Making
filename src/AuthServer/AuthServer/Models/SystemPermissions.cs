namespace AuthServer.Models;

/// <summary>
/// 预定义的系统权限
/// </summary>
public static class SystemPermissions
{
    // 用户管理权限
    public const string UserView = "user:view";
    public const string UserCreate = "user:create";
    public const string UserEdit = "user:edit";
    public const string UserDelete = "user:delete";
    public const string UserLock = "user:lock";
    public const string UserUnlock = "user:unlock";
    public const string UserExport = "user:export";
    public const string UserImport = "user:import";
    
    // 角色管理权限
    public const string RoleView = "role:view";
    public const string RoleCreate = "role:create";
    public const string RoleEdit = "role:edit";
    public const string RoleDelete = "role:delete";
    public const string RoleAssign = "role:assign";
    
    // 审计日志权限
    public const string AuditView = "audit:view";
    public const string AuditExport = "audit:export";
    
    // 客户端管理权限
    public const string ClientView = "client:view";
    public const string ClientCreate = "client:create";
    public const string ClientEdit = "client:edit";
    public const string ClientDelete = "client:delete";
    
    // 系统管理权限
    public const string SystemView = "system:view";
    public const string SystemConfig = "system:config";
    public const string SystemMaintenance = "system:maintenance";
    
    // 监控权限
    public const string MonitorView = "monitor:view";
    public const string MonitorManage = "monitor:manage";
    
    /// <summary>
    /// 获取所有系统权限
    /// </summary>
    public static IEnumerable<Permission> GetSystemPermissions()
    {
        return
        [
            // 用户管理
            new Permission { Id = Guid.NewGuid().ToString(), Name = UserView, Description = "查看用户", Group = "用户管理", Resource = "User", Action = "View", IsSystemPermission = true },
            new Permission { Id = Guid.NewGuid().ToString(), Name = UserCreate, Description = "创建用户", Group = "用户管理", Resource = "User", Action = "Create", IsSystemPermission = true },
            new Permission { Id = Guid.NewGuid().ToString(), Name = UserEdit, Description = "编辑用户", Group = "用户管理", Resource = "User", Action = "Edit", IsSystemPermission = true },
            new Permission { Id = Guid.NewGuid().ToString(), Name = UserDelete, Description = "删除用户", Group = "用户管理", Resource = "User", Action = "Delete", IsSystemPermission = true },
            new Permission { Id = Guid.NewGuid().ToString(), Name = UserLock, Description = "锁定用户", Group = "用户管理", Resource = "User", Action = "Lock", IsSystemPermission = true },
            new Permission { Id = Guid.NewGuid().ToString(), Name = UserUnlock, Description = "解锁用户", Group = "用户管理", Resource = "User", Action = "Unlock", IsSystemPermission = true },
            new Permission { Id = Guid.NewGuid().ToString(), Name = UserExport, Description = "导出用户", Group = "用户管理", Resource = "User", Action = "Export", IsSystemPermission = true },
            new Permission { Id = Guid.NewGuid().ToString(), Name = UserImport, Description = "导入用户", Group = "用户管理", Resource = "User", Action = "Import", IsSystemPermission = true },
            
            // 角色管理
            new Permission { Id = Guid.NewGuid().ToString(), Name = RoleView, Description = "查看角色", Group = "角色管理", Resource = "Role", Action = "View", IsSystemPermission = true },
            new Permission { Id = Guid.NewGuid().ToString(), Name = RoleCreate, Description = "创建角色", Group = "角色管理", Resource = "Role", Action = "Create", IsSystemPermission = true },
            new Permission { Id = Guid.NewGuid().ToString(), Name = RoleEdit, Description = "编辑角色", Group = "角色管理", Resource = "Role", Action = "Edit", IsSystemPermission = true },
            new Permission { Id = Guid.NewGuid().ToString(), Name = RoleDelete, Description = "删除角色", Group = "角色管理", Resource = "Role", Action = "Delete", IsSystemPermission = true },
            new Permission { Id = Guid.NewGuid().ToString(), Name = RoleAssign, Description = "分配角色", Group = "角色管理", Resource = "Role", Action = "Assign", IsSystemPermission = true },
            
            // 审计日志
            new Permission { Id = Guid.NewGuid().ToString(), Name = AuditView, Description = "查看审计日志", Group = "审计管理", Resource = "Audit", Action = "View", IsSystemPermission = true },
            new Permission { Id = Guid.NewGuid().ToString(), Name = AuditExport, Description = "导出审计日志", Group = "审计管理", Resource = "Audit", Action = "Export", IsSystemPermission = true },
            
            // 客户端管理
            new Permission { Id = Guid.NewGuid().ToString(), Name = ClientView, Description = "查看客户端", Group = "客户端管理", Resource = "Client", Action = "View", IsSystemPermission = true },
            new Permission { Id = Guid.NewGuid().ToString(), Name = ClientCreate, Description = "创建客户端", Group = "客户端管理", Resource = "Client", Action = "Create", IsSystemPermission = true },
            new Permission { Id = Guid.NewGuid().ToString(), Name = ClientEdit, Description = "编辑客户端", Group = "客户端管理", Resource = "Client", Action = "Edit", IsSystemPermission = true },
            new Permission { Id = Guid.NewGuid().ToString(), Name = ClientDelete, Description = "删除客户端", Group = "客户端管理", Resource = "Client", Action = "Delete", IsSystemPermission = true },
            
            // 系统管理
            new Permission { Id = Guid.NewGuid().ToString(), Name = SystemView, Description = "查看系统信息", Group = "系统管理", Resource = "System", Action = "View", IsSystemPermission = true },
            new Permission { Id = Guid.NewGuid().ToString(), Name = SystemConfig, Description = "系统配置", Group = "系统管理", Resource = "System", Action = "Config", IsSystemPermission = true },
            new Permission { Id = Guid.NewGuid().ToString(), Name = SystemMaintenance, Description = "系统维护", Group = "系统管理", Resource = "System", Action = "Maintenance", IsSystemPermission = true },
            
            // 监控管理
            new Permission { Id = Guid.NewGuid().ToString(), Name = MonitorView, Description = "查看监控", Group = "监控管理", Resource = "Monitor", Action = "View", IsSystemPermission = true },
            new Permission { Id = Guid.NewGuid().ToString(), Name = MonitorManage, Description = "管理监控", Group = "监控管理", Resource = "Monitor", Action = "Manage", IsSystemPermission = true }
        ];
    }
}