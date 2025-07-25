using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Making.AspNetCore;

/// <summary>
/// API 统一结果过滤器
/// </summary>
public class ApiResultFilter : IAsyncResultFilter
{
    /// <summary>
    /// 结果执行处理
    /// </summary>
    public async Task OnResultExecutionAsync(ResultExecutingContext context, ResultExecutionDelegate next)
    {
        if (ShouldSkipWrapper(context))
        {
            await next();
            return;
        }

        var (statusCode, value) = GetResultInfo(context);

        object wrappedResult;
        if (statusCode >= 200 && statusCode < 300)
        {
            // 成功响应 (2xx)
            wrappedResult = ResultDto.Success(value);
        }
        else
        {
            // 错误响应 (非2xx)
            wrappedResult = ResultDto.Error(
                message: GetErrorMessage(statusCode),
                data: value,
                code: statusCode
            );
        }

        context.Result = new ObjectResult(wrappedResult)
        {
            StatusCode = statusCode
        };

        await next();
    }

    /// <summary>
    /// 获取结果的状态码和值
    /// </summary>
    private (int statusCode, object? value) GetResultInfo(ResultExecutingContext context)
    {
        return context.Result switch
        {
            // 处理对象结果
            ObjectResult objectResult => (
                objectResult.StatusCode ?? context.HttpContext.Response.StatusCode,
                objectResult.Value
            ),

            // 处理 JSON 结果
            JsonResult jsonResult => (
                jsonResult.StatusCode ?? context.HttpContext.Response.StatusCode,
                jsonResult.Value
            ),

            // 处理内容结果
            ContentResult contentResult => (
                contentResult.StatusCode ?? context.HttpContext.Response.StatusCode,
                contentResult.Content
            ),

            // 处理状态码结果
            StatusCodeResult statusCodeResult => (
                statusCodeResult.StatusCode,
                null
            ),

            // 处理空结果
            EmptyResult => (
                context.HttpContext.Response.StatusCode != StatusCodes.Status200OK
                    ? context.HttpContext.Response.StatusCode
                    : StatusCodes.Status200OK,
                null
            ),

            // 默认处理
            _ => (
                context.HttpContext.Response.StatusCode,
                null
            )
        };
    }

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
    private bool ShouldSkipWrapper(ResultExecutingContext context)
    {
        var result = context.Result;

        // 特殊结果类型，跳过(文件、重定向、认证、登入登出、视图结果)
        if (result is FileResult ||
            result is RedirectResult ||
            result is ChallengeResult ||
            result is SignInResult ||
            result is SignOutResult ||
            result is ViewResult)
        {
            return true;
        }

        // 已经是 ResultDto 类型，跳过
        if (result is ObjectResult objResult && objResult.Value is ResultDto)
        {
            return true;
        }

        // 存在 SkipWrapAttribute 特性，跳过
        if (context.ActionDescriptor.EndpointMetadata.OfType<SkipWrapAttribute>().Any())
        {
            return true;
        }

        return false;
    }
}