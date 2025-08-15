using AuthServer.Models;
using AuthServer.Services;
using System.Text;

namespace AuthServer.Middleware;

/// <summary>
/// 审计日志中间件 - 自动记录请求和响应
/// </summary>
public class AuditMiddleware(RequestDelegate next, ILogger<AuditMiddleware> logger)
{
    private readonly HashSet<string> _auditPaths = new(StringComparer.OrdinalIgnoreCase)
    {
        "/admin",
        "/connect/token",
        "/connect/authorize",
        "/connect/authenticate",
        "/Account/Login",
        "/Account/Logout",
        "/Account/Register"
    };
    private readonly HashSet<string> _excludePaths = new(StringComparer.OrdinalIgnoreCase)
    {
        "/health",
        "/favicon.ico",
        "/_framework",
        "/css",
        "/js",
        "/images"
    };

    // 需要审计的路径
    // 排除的路径

    public async Task InvokeAsync(HttpContext context)
    {
        // 检查是否需要审计
        if (!ShouldAudit(context))
        {
            await next(context);
            return;
        }

        var startTime = DateTime.UtcNow;
        var originalBodyStream = context.Response.Body;
        
        using var responseBody = new MemoryStream();
        context.Response.Body = responseBody;

        try
        {
            await next(context);

            var duration = DateTime.UtcNow - startTime;
            await LogRequestAsync(context, duration, null);
        }
        catch (Exception ex)
        {
            var duration = DateTime.UtcNow - startTime;
            await LogRequestAsync(context, duration, ex);
            throw;
        }
        finally
        {
            responseBody.Seek(0, SeekOrigin.Begin);
            await responseBody.CopyToAsync(originalBodyStream);
            context.Response.Body = originalBodyStream;
        }
    }

    private bool ShouldAudit(HttpContext context)
    {
        var path = context.Request.Path.Value ?? "";
        
        // 排除静态资源和健康检查
        if (_excludePaths.Any(exclude => path.StartsWith(exclude, StringComparison.OrdinalIgnoreCase)))
        {
            return false;
        }
        
        // 只审计特定路径或POST/PUT/DELETE请求
        return _auditPaths.Any(audit => path.StartsWith(audit, StringComparison.OrdinalIgnoreCase)) ||
               context.Request.Method is "POST" or "PUT" or "DELETE" or "PATCH";
    }

    private async Task LogRequestAsync(HttpContext context, TimeSpan duration, Exception? exception)
    {
        try
        {
            var auditService = context.RequestServices.GetService<IAuditService>();
            if (auditService == null) return;

            var method = context.Request.Method;
            var path = context.Request.Path.Value ?? "";
            var statusCode = context.Response.StatusCode;
            var user = context.User;
            
            var action = $"HTTP_{method}";
            var description = $"{method} {path} - {statusCode}";
            var result = GetAuditResult(statusCode, exception);
            var riskLevel = GetRiskLevel(context, statusCode, exception);

            var additionalData = new
            {
                Method = method,
                Path = path,
                StatusCode = statusCode,
                Duration = duration.TotalMilliseconds,
                QueryString = context.Request.QueryString.ToString(),
                ContentType = context.Request.ContentType,
                ContentLength = context.Request.ContentLength,
                Exception = exception?.Message
            };

            if (IsAuthenticationPath(path))
            {
                await LogAuthenticationEventAsync(auditService, context, result, riskLevel, additionalData);
            }
            else
            {
                await auditService.LogAsync(action, "HTTP", path, description, result, riskLevel, additionalData);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to log audit information for request {Method} {Path}", 
                context.Request.Method, context.Request.Path);
        }
    }

    private static AuditResult GetAuditResult(int statusCode, Exception? exception)
    {
        if (exception != null) return AuditResult.Error;
        
        return statusCode switch
        {
            >= 200 and < 300 => AuditResult.Success,
            >= 300 and < 400 => AuditResult.Success,
            >= 400 and < 500 => AuditResult.Failed,
            >= 500 => AuditResult.Error,
            _ => AuditResult.Warning
        };
    }

    private static RiskLevel GetRiskLevel(HttpContext context, int statusCode, Exception? exception)
    {
        if (exception != null) return RiskLevel.High;
        
        var path = context.Request.Path.Value ?? "";
        
        // 认证和授权相关路径风险等级较高
        if (IsAuthenticationPath(path))
        {
            return statusCode >= 400 ? RiskLevel.Medium : RiskLevel.Low;
        }
        
        // 管理员路径风险等级较高
        if (path.StartsWith("/admin", StringComparison.OrdinalIgnoreCase))
        {
            return RiskLevel.Medium;
        }
        
        // 删除操作风险等级较高
        if (context.Request.Method == "DELETE")
        {
            return RiskLevel.Medium;
        }
        
        return statusCode >= 500 ? RiskLevel.High : RiskLevel.Low;
    }

    private static bool IsAuthenticationPath(string path)
    {
        var authPaths = new[]
        {
            "/connect/token",
            "/connect/authorize",
            "/connect/authenticate",
            "/Account/Login",
            "/Account/Logout",
            "/Account/Register",
            "/Account/ExternalLogin"
        };
        
        return authPaths.Any(authPath => path.StartsWith(authPath, StringComparison.OrdinalIgnoreCase));
    }

    private static async Task LogAuthenticationEventAsync(IAuditService auditService, HttpContext context, 
        AuditResult result, RiskLevel riskLevel, object additionalData)
    {
        var path = context.Request.Path.Value ?? "";
        var method = context.Request.Method;
        
        string action = path.ToLower() switch
        {
            var p when p.Contains("login") => method == "POST" ? "LOGIN_ATTEMPT" : "LOGIN_PAGE_ACCESS",
            var p when p.Contains("logout") => "LOGOUT_ATTEMPT",
            var p when p.Contains("register") => "REGISTER_ATTEMPT",
            var p when p.Contains("token") => "TOKEN_REQUEST",
            var p when p.Contains("authorize") => "AUTHORIZATION_REQUEST",
            _ => $"AUTH_{method}"
        };

        await auditService.LogAsync(action, "Authentication", path, $"Authentication request: {method} {path}",
            result, riskLevel, additionalData);
    }
}

/// <summary>
/// 审计中间件扩展方法
/// </summary>
public static class AuditMiddlewareExtensions
{
    public static IApplicationBuilder UseAuditLogging(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<AuditMiddleware>();
    }
}