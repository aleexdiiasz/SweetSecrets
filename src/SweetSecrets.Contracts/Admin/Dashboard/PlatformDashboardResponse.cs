namespace SweetSecrets.Contracts.Admin.Dashboard;

public sealed class PlatformDashboardResponse
{
    public int TotalTenants { get; set; }
    public IReadOnlyDictionary<string, int> TenantsByStatus { get; set; } = new Dictionary<string, int>();
    public int TotalUsers { get; set; }
    public int TenantOwners { get; set; }
    public int PlatformAdmins { get; set; }
    public int BlockedUsers { get; set; }
    public int PendingEmailConfirmation { get; set; }
    public int ActiveSessions { get; set; }
    public int OnlineUsers { get; set; }
    public IReadOnlyList<PlatformRecentTenantResponse> RecentTenants { get; set; } = [];
    public IReadOnlyList<PlatformRecentUserResponse> RecentUsers { get; set; } = [];
    public IReadOnlyList<PlatformRecentEventResponse> RecentEvents { get; set; } = [];
}

public sealed class PlatformRecentTenantResponse
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public sealed class PlatformRecentUserResponse
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public sealed class PlatformRecentEventResponse
{
    public string Action { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
