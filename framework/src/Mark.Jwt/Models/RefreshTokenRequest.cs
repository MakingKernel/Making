using System.ComponentModel.DataAnnotations;

namespace Mark.Jwt.Models;

/// <summary>
/// 刷新令牌请求
/// </summary>
public class RefreshTokenRequest
{
    /// <summary>
    /// 刷新令牌
    /// </summary>
    [Required(ErrorMessage = "刷新令牌不能为空")]
    public string RefreshToken { get; set; } = string.Empty;

    /// <summary>
    /// 客户端ID（可选）
    /// </summary>
    public string? ClientId { get; set; }

    /// <summary>
    /// 作用域（可选）
    /// </summary>
    public string? Scope { get; set; }
}

/// <summary>
/// 撤销令牌请求
/// </summary>
public class RevokeTokenRequest
{
    /// <summary>
    /// 要撤销的令牌（可以是访问令牌或刷新令牌）
    /// </summary>
    [Required(ErrorMessage = "令牌不能为空")]
    public string Token { get; set; } = string.Empty;

    /// <summary>
    /// 令牌类型提示
    /// </summary>
    public string? TokenTypeHint { get; set; }

    /// <summary>
    /// 客户端ID（可选）
    /// </summary>
    public string? ClientId { get; set; }
}

/// <summary>
/// 验证令牌请求
/// </summary>
public class ValidateTokenRequest
{
    /// <summary>
    /// 要验证的令牌
    /// </summary>
    [Required(ErrorMessage = "令牌不能为空")]
    public string Token { get; set; } = string.Empty;

    /// <summary>
    /// 是否验证过期时间
    /// </summary>
    public bool ValidateLifetime { get; set; } = true;

    /// <summary>
    /// 是否验证签发者
    /// </summary>
    public bool ValidateIssuer { get; set; } = true;

    /// <summary>
    /// 是否验证受众
    /// </summary>
    public bool ValidateAudience { get; set; } = true;
}
