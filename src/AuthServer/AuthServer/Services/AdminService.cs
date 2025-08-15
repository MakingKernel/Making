using System.Security.Claims;
using AuthServer.Dto;
using Making.AspNetCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuthServer.Services;

/// <summary>
/// 管理员服务 - 用户管理相关API
/// </summary>
[MiniApi(route: "/admin/users", Tags = "Admin - User Management")]
[Filter(typeof(ApiResultFilter))]
[Authorize] // 需要认证
public class AdminUserService
{
    private readonly IUserManagementService _userManagementService;
    private readonly ILogger<AdminUserService> _logger;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AdminUserService(
        IUserManagementService userManagementService,
        ILogger<AdminUserService> logger,
        IHttpContextAccessor httpContextAccessor)
    {
        _userManagementService = userManagementService;
        _logger = logger;
        _httpContextAccessor = httpContextAccessor;
    }

    private string? CurrentUserId => _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

    /// <summary>
    /// 获取用户列表
    /// </summary>
    [HttpGet]
    public async Task<PagedResult<UserDto>> GetUsers(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        [FromQuery] bool? isActive = null)
    {
        pageSize = Math.Min(pageSize, 100); // 限制最大页面大小
        return await _userManagementService.GetUsersAsync(page, pageSize, search, isActive);
    }

    /// <summary>
    /// 获取用户详情
    /// </summary>
    [HttpGet("{userId}")]
    public async Task<IResult> GetUser([FromRoute] string userId)
    {
        var user = await _userManagementService.GetUserAsync(userId);
        if (user == null)
        {
            return Results.NotFound(new { message = "用户不存在" });
        }

        return Results.Ok(user);
    }

    /// <summary>
    /// 创建用户
    /// </summary>
    [HttpPost]
    public async Task<IResult> CreateUser([FromBody] CreateUserRequest request)
    {
        try
        {
            var user = await _userManagementService.CreateUserAsync(request, CurrentUserId);
            return Results.Created($"/admin/users/{user.Id}", user);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "创建用户失败: {Email}", request.Email);
            return Results.BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// 更新用户
    /// </summary>
    [HttpPut("{userId}")]
    public async Task<IResult> UpdateUser([FromRoute] string userId, [FromBody] UpdateUserRequestDto requestDto)
    {
        try
        {
            var user = await _userManagementService.UpdateUserAsync(userId, requestDto, CurrentUserId);
            return Results.Ok(user);
        }
        catch (ArgumentException)
        {
            return Results.NotFound(new { message = "用户不存在" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新用户失败: {UserId}", userId);
            return Results.BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// 删除用户
    /// </summary>
    [HttpDelete("{userId}")]
    public async Task<IResult> DeleteUser([FromRoute] string userId)
    {
        var success = await _userManagementService.DeleteUserAsync(userId, CurrentUserId);
        if (!success)
        {
            return Results.NotFound(new { message = "用户不存在" });
        }

        return Results.Ok(new { message = "用户删除成功" });
    }

    /// <summary>
    /// 锁定用户
    /// </summary>
    [HttpPost("{userId}/lock")]
    public async Task<IResult> LockUser([FromRoute] string userId)
    {
        var success = await _userManagementService.LockUserAsync(userId, CurrentUserId);
        if (!success)
        {
            return Results.NotFound(new { message = "用户不存在" });
        }

        return Results.Ok(new { message = "用户锁定成功" });
    }

    /// <summary>
    /// 解锁用户
    /// </summary>
    [HttpPost("{userId}/unlock")]
    public async Task<IResult> UnlockUser([FromRoute] string userId)
    {
        var success = await _userManagementService.UnlockUserAsync(userId, CurrentUserId);
        if (!success)
        {
            return Results.NotFound(new { message = "用户不存在" });
        }

        return Results.Ok(new { message = "用户解锁成功" });
    }

    /// <summary>
    /// 激活用户
    /// </summary>
    [HttpPost("{userId}/activate")]
    public async Task<IResult> ActivateUser([FromRoute] string userId)
    {
        var success = await _userManagementService.ActivateUserAsync(userId, CurrentUserId);
        if (!success)
        {
            return Results.NotFound(new { message = "用户不存在" });
        }

        return Results.Ok(new { message = "用户激活成功" });
    }

    /// <summary>
    /// 停用用户
    /// </summary>
    [HttpPost("{userId}/deactivate")]
    public async Task<IResult> DeactivateUser([FromRoute] string userId)
    {
        var success = await _userManagementService.DeactivateUserAsync(userId, CurrentUserId);
        if (!success)
        {
            return Results.NotFound(new { message = "用户不存在" });
        }

        return Results.Ok(new { message = "用户停用成功" });
    }

    /// <summary>
    /// 重置用户密码
    /// </summary>
    [HttpPost("{userId}/reset-password")]
    public async Task<IResult> ResetPassword([FromRoute] string userId)
    {
        var success = await _userManagementService.ResetPasswordAsync(userId, CurrentUserId);
        if (!success)
        {
            return Results.NotFound(new { message = "用户不存在" });
        }

        return Results.Ok(new { message = "密码重置成功，临时密码已记录在审计日志中" });
    }

    /// <summary>
    /// 获取用户角色
    /// </summary>
    [HttpGet("{userId}/roles")]
    public async Task<IResult> GetUserRoles([FromRoute] string userId)
    {
        var roles = await _userManagementService.GetUserRolesAsync(userId);
        return Results.Ok(roles);
    }

    /// <summary>
    /// 分配角色
    /// </summary>
    [HttpPost("{userId}/roles")]
    public async Task<IResult> AssignRoles([FromRoute] string userId, [FromBody] AssignRolesRequest request)
    {
        var success = await _userManagementService.AssignRolesAsync(userId, request.RoleNames, CurrentUserId);
        if (!success)
        {
            return Results.NotFound(new { message = "用户不存在" });
        }

        return Results.Ok(new { message = "角色分配成功" });
    }

    /// <summary>
    /// 移除角色
    /// </summary>
    [HttpDelete("{userId}/roles")]
    public async Task<IResult> RemoveRoles([FromRoute] string userId, [FromBody] RemoveRolesRequest request)
    {
        var success = await _userManagementService.RemoveRolesAsync(userId, request.RoleNames, CurrentUserId);
        if (!success)
        {
            return Results.NotFound(new { message = "用户不存在" });
        }

        return Results.Ok(new { message = "角色移除成功" });
    }

    /// <summary>
    /// 获取用户统计信息
    /// </summary>
    [HttpGet("stats")]
    public async Task<UserStatsDto> GetUserStats()
    {
        return await _userManagementService.GetUserStatsAsync();
    }
}

#region Request Models

/// <summary>
/// 分配角色请求
/// </summary>
public class AssignRolesRequest
{
    public IEnumerable<string> RoleNames { get; set; } = new List<string>();
}

/// <summary>
/// 移除角色请求
/// </summary>
public class RemoveRolesRequest
{
    public IEnumerable<string> RoleNames { get; set; } = new List<string>();
}

#endregion