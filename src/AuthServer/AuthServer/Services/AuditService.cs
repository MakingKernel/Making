using AuthServer.Data;
using AuthServer.Models;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text.Json;

namespace AuthServer.Services;

/// <summary>
/// 审计日志服务
/// </summary>
public interface IAuditService
{
    Task LogAsync(string action, string? resourceType = null, string? resourceId = null, 
        string? description = null, AuditResult result = AuditResult.Success, 
        RiskLevel riskLevel = RiskLevel.Low, object? additionalData = null);
        
    Task LogUserActionAsync(string userId, string action, string? resourceType = null, 
        string? resourceId = null, string? description = null, AuditResult result = AuditResult.Success, 
        RiskLevel riskLevel = RiskLevel.Low, object? additionalData = null);
        
    Task LogLoginAsync(string userId, string? userName, bool success, string? reason = null, RiskLevel riskLevel = RiskLevel.Low);
    Task LogLogoutAsync(string userId, string? userName);
    Task LogSecurityEventAsync(string action, string? description, RiskLevel riskLevel = RiskLevel.High, object? additionalData = null);
    
    Task<IEnumerable<AuditLog>> GetAuditLogsAsync(int page = 1, int pageSize = 50, string? userId = null, 
        string? action = null, DateTime? startDate = null, DateTime? endDate = null);
        
    Task<int> GetAuditLogsCountAsync(string? userId = null, string? action = null, 
        DateTime? startDate = null, DateTime? endDate = null);
}

public class AuditService : IAuditService
{
    private readonly ApplicationDbContext _context;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<AuditService> _logger;

    public AuditService(
        ApplicationDbContext context,
        IHttpContextAccessor httpContextAccessor,
        ILogger<AuditService> logger)
    {
        _context = context;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    public async Task LogAsync(string action, string? resourceType = null, string? resourceId = null,
        string? description = null, AuditResult result = AuditResult.Success, 
        RiskLevel riskLevel = RiskLevel.Low, object? additionalData = null)
    {
        try
        {
            var httpContext = _httpContextAccessor.HttpContext;
            var user = httpContext?.User;
            
            var auditLog = new AuditLog
            {
                UserId = user?.FindFirst(ClaimTypes.NameIdentifier)?.Value,
                UserName = user?.Identity?.Name,
                Action = action,
                ResourceType = resourceType,
                ResourceId = resourceId,
                Description = description,
                Result = result,
                RiskLevel = riskLevel,
                IpAddress = GetClientIpAddress(httpContext),
                UserAgent = httpContext?.Request.Headers["User-Agent"].ToString(),
                Source = "AuthServer",
                SessionId = httpContext?.Session?.Id,
                AdditionalData = additionalData != null ? JsonSerializer.Serialize(additionalData) : null
            };

            _context.AuditLogs.Add(auditLog);
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to write audit log for action: {Action}", action);
        }
    }

    public async Task LogUserActionAsync(string userId, string action, string? resourceType = null,
        string? resourceId = null, string? description = null, AuditResult result = AuditResult.Success,
        RiskLevel riskLevel = RiskLevel.Low, object? additionalData = null)
    {
        try
        {
            var httpContext = _httpContextAccessor.HttpContext;
            var user = await _context.Users.FindAsync(userId);
            
            var auditLog = new AuditLog
            {
                UserId = userId,
                UserName = user?.UserName,
                Action = action,
                ResourceType = resourceType,
                ResourceId = resourceId,
                Description = description,
                Result = result,
                RiskLevel = riskLevel,
                IpAddress = GetClientIpAddress(httpContext),
                UserAgent = httpContext?.Request.Headers["User-Agent"].ToString(),
                Source = "AuthServer",
                SessionId = httpContext?.Session?.Id,
                AdditionalData = additionalData != null ? JsonSerializer.Serialize(additionalData) : null
            };

            _context.AuditLogs.Add(auditLog);
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to write audit log for user action: {Action}", action);
        }
    }

    public async Task LogLoginAsync(string userId, string? userName, bool success, string? reason = null, RiskLevel riskLevel = RiskLevel.Low)
    {
        var action = success ? AuditActions.LoginSuccess : AuditActions.LoginFailed;
        var result = success ? AuditResult.Success : AuditResult.Failed;
        var description = success ? "用户登录成功" : $"用户登录失败: {reason}";
        
        if (!success)
        {
            riskLevel = RiskLevel.Medium; // 登录失败风险等级提升
        }

        await LogUserActionAsync(userId, action, "Authentication", userId, description, result, riskLevel,
            new { Reason = reason, LoginTime = DateTime.UtcNow });
    }

    public async Task LogLogoutAsync(string userId, string? userName)
    {
        await LogUserActionAsync(userId, AuditActions.Logout, "Authentication", userId, "用户登出",
            AuditResult.Success, RiskLevel.Low, new { LogoutTime = DateTime.UtcNow });
    }

    public async Task LogSecurityEventAsync(string action, string? description, RiskLevel riskLevel = RiskLevel.High, object? additionalData = null)
    {
        await LogAsync(action, "Security", null, description, AuditResult.Warning, riskLevel, additionalData);
    }

    public async Task<IEnumerable<AuditLog>> GetAuditLogsAsync(int page = 1, int pageSize = 50, string? userId = null,
        string? action = null, DateTime? startDate = null, DateTime? endDate = null)
    {
        var query = _context.AuditLogs.AsQueryable();

        if (!string.IsNullOrEmpty(userId))
        {
            query = query.Where(a => a.UserId == userId);
        }

        if (!string.IsNullOrEmpty(action))
        {
            query = query.Where(a => a.Action.Contains(action));
        }

        if (startDate.HasValue)
        {
            query = query.Where(a => a.Timestamp >= startDate.Value);
        }

        if (endDate.HasValue)
        {
            query = query.Where(a => a.Timestamp <= endDate.Value);
        }

        return await query
            .OrderByDescending(a => a.Timestamp)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<int> GetAuditLogsCountAsync(string? userId = null, string? action = null,
        DateTime? startDate = null, DateTime? endDate = null)
    {
        var query = _context.AuditLogs.AsQueryable();

        if (!string.IsNullOrEmpty(userId))
        {
            query = query.Where(a => a.UserId == userId);
        }

        if (!string.IsNullOrEmpty(action))
        {
            query = query.Where(a => a.Action.Contains(action));
        }

        if (startDate.HasValue)
        {
            query = query.Where(a => a.Timestamp >= startDate.Value);
        }

        if (endDate.HasValue)
        {
            query = query.Where(a => a.Timestamp <= endDate.Value);
        }

        return await query.CountAsync();
    }

    private static string? GetClientIpAddress(HttpContext? context)
    {
        if (context == null) return null;
        
        // 检查是否有代理转发的IP
        var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedFor))
        {
            var ips = forwardedFor.Split(',', StringSplitOptions.RemoveEmptyEntries);
            if (ips.Length > 0)
            {
                return ips[0].Trim();
            }
        }

        var realIp = context.Request.Headers["X-Real-IP"].FirstOrDefault();
        if (!string.IsNullOrEmpty(realIp))
        {
            return realIp;
        }

        return context.Connection.RemoteIpAddress?.ToString();
    }
}