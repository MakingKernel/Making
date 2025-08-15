using AuthServer.Data;
using AuthServer.Dto;
using AuthServer.Models;
using Making.AspNetCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace AuthServer.Services;

/// <summary>
/// 用户管理服务实现
/// </summary>
public class UserManagementService : IUserManagementService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<AdminRole> _roleManager;
    private readonly ApplicationDbContext _context;
    private readonly IAuditService _auditService;
    private readonly ILogger<UserManagementService> _logger;

    public UserManagementService(
        UserManager<ApplicationUser> userManager,
        RoleManager<AdminRole> roleManager,
        ApplicationDbContext context,
        IAuditService auditService,
        ILogger<UserManagementService> logger)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _context = context;
        _auditService = auditService;
        _logger = logger;
    }

    public async Task<PagedResult<UserDto>> GetUsersAsync(int page, int pageSize, string? search = null,
        bool? isActive = null)
    {
        var query = _context.Users.AsQueryable();

        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(u => u.UserName!.Contains(search) ||
                                     u.Email!.Contains(search) ||
                                     u.FirstName!.Contains(search) ||
                                     u.LastName!.Contains(search));
        }

        if (isActive.HasValue)
        {
            query = query.Where(u => u.IsActive == isActive.Value);
        }

        var totalCount = await query.CountAsync();
        var users = await query
            .OrderByDescending(u => u.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(u => new UserDto
            {
                Id = u.Id,
                UserName = u.UserName!,
                Email = u.Email!,
                FirstName = u.FirstName,
                LastName = u.LastName,
                DisplayName = u.DisplayName,
                Avatar = u.Avatar,
                Department = u.Department,
                Position = u.Position,
                IsActive = u.IsActive,
                IsAdmin = u.IsAdmin,
                EmailConfirmed = u.EmailConfirmed,
                PhoneNumber = u.PhoneNumber,
                TwoFactorEnabled = u.TwoFactorEnabled,
                LockoutEnd = u.LockoutEnd,
                AccessFailedCount = u.AccessFailedCount,
                LastLoginAt = u.LastLoginAt,
                LastLoginIp = u.LastLoginIp,
                LoginCount = u.LoginCount,
                CreatedAt = u.CreatedAt,
                UpdatedAt = u.UpdatedAt,
                CreatedBy = u.CreatedBy,
                Notes = u.Notes
            })
            .ToListAsync();

        return new PagedResult<UserDto>(new PagingModel()
        {
            TotalCount = totalCount,
            PageIndex = page,
            PageSize = pageSize
        }, users);
    }

    public async Task<UserDto?> GetUserAsync(string userId)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null) return null;

        var roles = await _userManager.GetRolesAsync(user);

        return new UserDto
        {
            Id = user.Id,
            UserName = user.UserName!,
            Email = user.Email!,
            FirstName = user.FirstName,
            LastName = user.LastName,
            DisplayName = user.DisplayName,
            Avatar = user.Avatar,
            Department = user.Department,
            Position = user.Position,
            IsActive = user.IsActive,
            IsAdmin = user.IsAdmin,
            EmailConfirmed = user.EmailConfirmed,
            PhoneNumber = user.PhoneNumber,
            TwoFactorEnabled = user.TwoFactorEnabled,
            LockoutEnd = user.LockoutEnd,
            AccessFailedCount = user.AccessFailedCount,
            LastLoginAt = user.LastLoginAt,
            LastLoginIp = user.LastLoginIp,
            LoginCount = user.LoginCount,
            CreatedAt = user.CreatedAt,
            UpdatedAt = user.UpdatedAt,
            CreatedBy = user.CreatedBy,
            Notes = user.Notes,
            Roles = roles.ToList()
        };
    }

    public async Task<UserDto> CreateUserAsync(CreateUserRequest request, string? createdBy = null)
    {
        var user = new ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email,
            FirstName = request.FirstName,
            LastName = request.LastName,
            Department = request.Department,
            Position = request.Position,
            IsActive = request.IsActive,
            IsAdmin = request.IsAdmin,
            EmailConfirmed = request.EmailConfirmed,
            CreatedBy = createdBy,
            Notes = request.Notes
        };

        var result = await _userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            throw new InvalidOperationException(
                $"创建用户失败: {string.Join(", ", result.Errors.Select(e => e.Description))}");
        }

        // 分配角色
        if (request.Roles?.Any() == true)
        {
            await _userManager.AddToRolesAsync(user, request.Roles);
        }

        await _auditService.LogAsync(AuditActions.UserCreated, "User", user.Id,
            $"创建用户: {user.Email}", AuditResult.Success, RiskLevel.Medium,
            new { UserEmail = user.Email, IsAdmin = user.IsAdmin, Roles = request.Roles });

        return (await GetUserAsync(user.Id))!;
    }

    public async Task<UserDto> UpdateUserAsync(string userId, UpdateUserRequestDto requestDto, string? updatedBy = null)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            throw new ArgumentException("用户不存在", nameof(userId));
        }

        var originalData = new
        {
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            IsActive = user.IsActive,
            IsAdmin = user.IsAdmin
        };

        user.FirstName = requestDto.FirstName;
        user.LastName = requestDto.LastName;
        user.Department = requestDto.Department;
        user.Position = requestDto.Position;
        user.IsActive = requestDto.IsActive;
        user.IsAdmin = requestDto.IsAdmin;
        user.UpdatedAt = DateTime.UtcNow;
        user.UpdatedBy = updatedBy;
        user.Notes = requestDto.Notes;

        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            throw new InvalidOperationException(
                $"更新用户失败: {string.Join(", ", result.Errors.Select(e => e.Description))}");
        }

        await _auditService.LogAsync(AuditActions.UserUpdated, "User", user.Id,
            $"更新用户: {user.Email}", AuditResult.Success, RiskLevel.Low,
            new { Original = originalData, Updated = requestDto });

        return (await GetUserAsync(user.Id))!;
    }

    public async Task<bool> DeleteUserAsync(string userId, string? deletedBy = null)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return false;

        var result = await _userManager.DeleteAsync(user);
        if (result.Succeeded)
        {
            await _auditService.LogAsync(AuditActions.UserDeleted, "User", userId,
                $"删除用户: {user.Email}", AuditResult.Success, RiskLevel.High,
                new { UserEmail = user.Email, DeletedBy = deletedBy });
        }

        return result.Succeeded;
    }

    public async Task<bool> LockUserAsync(string userId, string? lockedBy = null)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return false;

        var result = await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.UtcNow.AddYears(100));
        if (result.Succeeded)
        {
            await _auditService.LogAsync(AuditActions.UserLocked, "User", userId,
                $"锁定用户: {user.Email}", AuditResult.Success, RiskLevel.Medium,
                new { UserEmail = user.Email, LockedBy = lockedBy });
        }

        return result.Succeeded;
    }

    public async Task<bool> UnlockUserAsync(string userId, string? unlockedBy = null)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return false;

        var result = await _userManager.SetLockoutEndDateAsync(user, null);
        if (result.Succeeded)
        {
            await _auditService.LogAsync(AuditActions.UserUnlocked, "User", userId,
                $"解锁用户: {user.Email}", AuditResult.Success, RiskLevel.Low,
                new { UserEmail = user.Email, UnlockedBy = unlockedBy });
        }

        return result.Succeeded;
    }

    public async Task<bool> ActivateUserAsync(string userId, string? activatedBy = null)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return false;

        user.IsActive = true;
        user.UpdatedAt = DateTime.UtcNow;
        user.UpdatedBy = activatedBy;

        var result = await _userManager.UpdateAsync(user);
        if (result.Succeeded)
        {
            await _auditService.LogAsync(AuditActions.UserActivated, "User", userId,
                $"激活用户: {user.Email}", AuditResult.Success, RiskLevel.Low,
                new { UserEmail = user.Email, ActivatedBy = activatedBy });
        }

        return result.Succeeded;
    }

    public async Task<bool> DeactivateUserAsync(string userId, string? deactivatedBy = null)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return false;

        user.IsActive = false;
        user.UpdatedAt = DateTime.UtcNow;
        user.UpdatedBy = deactivatedBy;

        var result = await _userManager.UpdateAsync(user);
        if (result.Succeeded)
        {
            await _auditService.LogAsync(AuditActions.UserDeactivated, "User", userId,
                $"停用用户: {user.Email}", AuditResult.Success, RiskLevel.Medium,
                new { UserEmail = user.Email, DeactivatedBy = deactivatedBy });
        }

        return result.Succeeded;
    }

    public async Task<bool> ResetPasswordAsync(string userId, string? resetBy = null)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return false;

        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        var newPassword = GenerateTemporaryPassword();
        var result = await _userManager.ResetPasswordAsync(user, token, newPassword);

        if (result.Succeeded)
        {
            await _auditService.LogAsync(AuditActions.PasswordReset, "User", userId,
                $"重置用户密码: {user.Email}", AuditResult.Success, RiskLevel.Medium,
                new { UserEmail = user.Email, ResetBy = resetBy, TemporaryPassword = newPassword });
        }

        return result.Succeeded;
    }

    public async Task<IEnumerable<string>> GetUserRolesAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return new List<string>();

        return await _userManager.GetRolesAsync(user);
    }

    public async Task<bool> AssignRolesAsync(string userId, IEnumerable<string> roleNames, string? assignedBy = null)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return false;

        var existingRoles = await _userManager.GetRolesAsync(user);
        var newRoles = roleNames.Except(existingRoles).ToList();

        if (newRoles.Any())
        {
            var result = await _userManager.AddToRolesAsync(user, newRoles);
            if (result.Succeeded)
            {
                await _auditService.LogAsync(AuditActions.RoleAssigned, "User", userId,
                    $"为用户 {user.Email} 分配角色: {string.Join(", ", newRoles)}", AuditResult.Success, RiskLevel.Medium,
                    new { UserEmail = user.Email, NewRoles = newRoles, AssignedBy = assignedBy });
            }

            return result.Succeeded;
        }

        return true;
    }

    public async Task<bool> RemoveRolesAsync(string userId, IEnumerable<string> roleNames, string? removedBy = null)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return false;

        var result = await _userManager.RemoveFromRolesAsync(user, roleNames);
        if (result.Succeeded)
        {
            await _auditService.LogAsync(AuditActions.RoleRemoved, "User", userId,
                $"移除用户 {user.Email} 的角色: {string.Join(", ", roleNames)}", AuditResult.Success, RiskLevel.Medium,
                new { UserEmail = user.Email, RemovedRoles = roleNames, RemovedBy = removedBy });
        }

        return result.Succeeded;
    }

    public async Task<UserStatsDto> GetUserStatsAsync()
    {
        var totalUsers = await _context.Users.CountAsync();
        var activeUsers = await _context.Users.CountAsync(u => u.IsActive);
        var adminUsers = await _context.Users.CountAsync(u => u.IsAdmin);
        var lockedUsers =
            await _context.Users.CountAsync(u => u.LockoutEnd != null && u.LockoutEnd > DateTimeOffset.UtcNow);
        var recentlyCreated = await _context.Users.CountAsync(u => u.CreatedAt >= DateTime.UtcNow.AddDays(-7));

        return new UserStatsDto
        {
            TotalUsers = totalUsers,
            ActiveUsers = activeUsers,
            InactiveUsers = totalUsers - activeUsers,
            AdminUsers = adminUsers,
            LockedUsers = lockedUsers,
            RecentlyCreated = recentlyCreated
        };
    }

    private static string GenerateTemporaryPassword()
    {
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnpqrstuvwxyz23456789!@#$%^&*";
        var random = new Random();
        return new string(Enumerable.Repeat(chars, 12)
            .Select(s => s[random.Next(s.Length)]).ToArray());
    }
}