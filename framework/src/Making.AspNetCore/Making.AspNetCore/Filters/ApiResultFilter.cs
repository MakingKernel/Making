using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Making.AspNetCore;

/// <summary>
/// API 统一结果过滤器
/// </summary>
public class ApiResultFilter : IEndpointFilter
{
    /// <summary>
    /// 根据状态码获取错误消息
    /// </summary>
    private string GetErrorMessage(int statusCode)
    {
        return statusCode switch
        {
            StatusCodes.Status400BadRequest => "请求参数错误",
            StatusCodes.Status401Unauthorized => "未授权访问",
            StatusCodes.Status403Forbidden => "访问被禁止",
            StatusCodes.Status404NotFound => "资源不存在",
            StatusCodes.Status500InternalServerError => "服务器内部错误",
            _ => $"请求失败，状态码: {statusCode}"
        };
    }

    /// <summary>
    /// 判断是否需要跳过结果包装
    /// </summary>
    private bool ShouldSkipWrapper(object result)
    {
        if (result is FileResult or RedirectResult or ChallengeResult or SignInResult or SignOutResult or ViewResult)
        {
            return true;
        }

        // 已经是 ResultDto 类型，跳过
        if (result is ObjectResult { Value: ResultDto })
        {
            return true;
        }

        if (result is IResult resultObj)
        {
            // 如果是 IResult 接口类型，直接返回
            return true;
        }

        return false;
    }

    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var value = await next(context);

        if (ShouldSkipWrapper(value))
        {
            return value;
        }

        return ResultDto.Success(value);
    }
}