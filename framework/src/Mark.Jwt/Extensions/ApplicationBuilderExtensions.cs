using Microsoft.AspNetCore.Builder;

namespace Mark.Jwt.Extensions;

/// <summary>
/// 应用程序构建器扩展方法
/// </summary>
public static class ApplicationBuilderExtensions
{
    /// <summary>
    /// 使用JWT认证和授权
    /// </summary>
    /// <param name="app">应用程序构建器</param>
    /// <returns></returns>
    public static IApplicationBuilder UseJwtAuthentication(this IApplicationBuilder app)
    {
        return app
            .UseAuthentication()
            .UseAuthorization();
    }
}
