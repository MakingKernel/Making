namespace AuthServer.Services;

/// <summary>
/// 用户统计数据
/// </summary>
public class UserStatsDto
{
    public int TotalUsers { get; set; }
    public int ActiveUsers { get; set; }
    public int InactiveUsers { get; set; }
    public int AdminUsers { get; set; }
    public int LockedUsers { get; set; }
    public int RecentlyCreated { get; set; }
}