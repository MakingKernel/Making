namespace AuthServer.Services;

/// <summary>
/// 更新用户请求
/// </summary>
public class UpdateUserRequestDto
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Department { get; set; }
    public string? Position { get; set; }
    public bool IsActive { get; set; }
    public bool IsAdmin { get; set; }
    public string? Notes { get; set; }
}