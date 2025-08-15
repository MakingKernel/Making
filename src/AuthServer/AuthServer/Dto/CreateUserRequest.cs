using System.ComponentModel.DataAnnotations;

namespace AuthServer.Services;

/// <summary>
/// 创建用户请求
/// </summary>
public class CreateUserRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MinLength(6)]
    public string Password { get; set; } = string.Empty;

    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Department { get; set; }
    public string? Position { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsAdmin { get; set; } = false;
    public bool EmailConfirmed { get; set; } = true;
    public string? Notes { get; set; }
    public List<string>? Roles { get; set; }
}