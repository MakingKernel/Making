using AuthServer.Dto;
using Making.AspNetCore;

namespace AuthServer.Services;

/// <summary>
/// 用户管理服务接口
/// </summary>
public interface IUserManagementService
{
    Task<PagedResult<UserDto>> GetUsersAsync(int page, int pageSize, string? search = null, bool? isActive = null);
    Task<UserDto?> GetUserAsync(string userId);
    Task<UserDto> CreateUserAsync(CreateUserRequest request, string? createdBy = null);
    Task<UserDto> UpdateUserAsync(string userId, UpdateUserRequestDto requestDto, string? updatedBy = null);
    Task<bool> DeleteUserAsync(string userId, string? deletedBy = null);
    Task<bool> LockUserAsync(string userId, string? lockedBy = null);
    Task<bool> UnlockUserAsync(string userId, string? unlockedBy = null);
    Task<bool> ActivateUserAsync(string userId, string? activatedBy = null);
    Task<bool> DeactivateUserAsync(string userId, string? deactivatedBy = null);
    Task<bool> ResetPasswordAsync(string userId, string? resetBy = null);
    Task<IEnumerable<string>> GetUserRolesAsync(string userId);
    Task<bool> AssignRolesAsync(string userId, IEnumerable<string> roleNames, string? assignedBy = null);
    Task<bool> RemoveRolesAsync(string userId, IEnumerable<string> roleNames, string? removedBy = null);
    Task<UserStatsDto> GetUserStatsAsync();
}