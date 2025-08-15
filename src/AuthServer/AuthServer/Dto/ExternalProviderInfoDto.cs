namespace AuthServer.Dto;

/// <summary>
/// 外部提供商信息
/// </summary>
public class ExternalProviderInfoDto
{
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Provider { get; set; } = string.Empty;
}