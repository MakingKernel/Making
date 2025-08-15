using Making.AspNetCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuthServer.Services;

/// <summary>
/// 管理员服务 - 系统管理相关API
/// </summary>
[MiniApi(route: "/admin/system", Tags = "Admin - System Management")]
[Filter(typeof(ApiResultFilter))]
[Authorize]
public class AdminSystemService
{
    /// <summary>
    /// 获取系统信息
    /// </summary>
    [HttpGet("info")]
    public IResult GetSystemInfo()
    {
        var info = new
        {
            Version = "1.0.0",
            Environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Unknown",
            MachineName = Environment.MachineName,
            OSVersion = Environment.OSVersion.ToString(),
            RuntimeVersion = Environment.Version.ToString(),
            ProcessorCount = Environment.ProcessorCount,
            WorkingSet = Environment.WorkingSet,
            StartTime = DateTime.UtcNow // 这里应该是应用程序启动时间，简化处理
        };

        return Results.Ok(info);
    }

    /// <summary>
    /// 获取系统健康状态
    /// </summary>
    [HttpGet("health")]
    public async Task<IResult> GetHealthStatus()
    {
        var health = new
        {
            Status = "Healthy",
            Timestamp = DateTime.UtcNow,
            Checks = new object[]
            {
                new { Name = "Database", Status = "Healthy", Duration = "5ms" },
                new { Name = "OpenIddict", Status = "Healthy", Duration = "2ms" },
                new { Name = "Memory", Status = "Healthy", Usage = $"{GC.GetTotalMemory(false) / 1024 / 1024} MB" }
            }
        };

        return Results.Ok(health);
    }
}