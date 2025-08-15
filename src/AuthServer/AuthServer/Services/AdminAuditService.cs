using System.Text;
using AuthServer.Models;
using Making.AspNetCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuthServer.Services;

/// <summary>
/// 管理员服务 - 审计日志相关API
/// </summary>
//[MiniApi(route: "/admin/audit", Tags = "Admin - Audit Log")]
[Filter(typeof(ApiResultFilter))]
[Authorize]
public class AdminAuditService(IAuditService auditService, ILogger<AdminAuditService> logger)
{
    /// <summary>
    /// 获取审计日志
    /// </summary>
    [HttpGet]
    public async Task<IResult> GetAuditLogs(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        [FromQuery] string? userId = null,
        [FromQuery] string? action = null,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null)
    {
        pageSize = Math.Min(pageSize, 200); // 限制最大页面大小

        var logs = await auditService.GetAuditLogsAsync(page, pageSize, userId, action, startDate, endDate);
        var totalCount = await auditService.GetAuditLogsCountAsync(userId, action, startDate, endDate);

        var result = new PagedResult<AuditLog>(new PagingModel()
        {
            PageIndex = page,
            PageSize = pageSize,
            TotalCount = totalCount
        }, logs.ToArray());

        return Results.Ok(result);
    }

    /// <summary>
    /// 导出审计日志
    /// </summary>
    [HttpGet("export")]
    public async Task<IResult> ExportAuditLogs(
        [FromQuery] string? userId = null,
        [FromQuery] string? action = null,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        [FromQuery] string format = "csv")
    {
        try
        {
            // 限制导出范围，防止数据过大
            var maxExportCount = 10000;
            var logs = await auditService.GetAuditLogsAsync(1, maxExportCount, userId, action, startDate, endDate);

            if (format.ToLower() == "csv")
            {
                var csv = GenerateCsv(logs);
                var fileName = $"audit_logs_{DateTime.UtcNow:yyyyMMdd_HHmmss}.csv";
                return Results.File(
                    Encoding.UTF8.GetBytes(csv),
                    "text/csv",
                    fileName);
            }

            return Results.BadRequest(new { message = "不支持的导出格式" });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "导出审计日志失败");
            return Results.Problem("导出失败");
        }
    }

    private static string GenerateCsv(IEnumerable<AuditLog> logs)
    {
        var csv = new StringBuilder();
        csv.AppendLine("时间,用户ID,用户名,操作,资源类型,资源ID,描述,结果,风险等级,IP地址");

        foreach (var log in logs)
        {
            csv.AppendLine(
                $"{log.Timestamp:yyyy-MM-dd HH:mm:ss},{log.UserId},{log.UserName},{log.Action},{log.ResourceType},{log.ResourceId},\"{log.Description}\",{log.Result},{log.RiskLevel},{log.IpAddress}");
        }

        return csv.ToString();
    }
}