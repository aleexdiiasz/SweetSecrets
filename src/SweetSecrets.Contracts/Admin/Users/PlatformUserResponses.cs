namespace SweetSecrets.Contracts.Admin.Users;

public sealed class PlatformUserPageResponse
{
    public IReadOnlyList<PlatformUserListItemResponse> Items { get; set; } = [];
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}

public class PlatformUserListItemResponse
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string? TenantCode { get; set; }
    public string? TenantName { get; set; }
    public bool IsActive { get; set; }
    public bool IsBlocked { get; set; }
    public bool EmailConfirmed { get; set; }
    public bool IsOnline { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public DateTime? LastActivityAt { get; set; }
    public DateTime CreatedAt { get; set; }
}

public sealed class PlatformUserDetailResponse : PlatformUserListItemResponse
{
    public IReadOnlyList<PlatformSessionItemResponse> RecentSessions { get; set; } = [];
    public IReadOnlyList<PlatformUserAuditItemResponse> RecentAudit { get; set; } = [];
}

public sealed class PlatformSessionPageResponse
{
    public IReadOnlyList<PlatformSessionItemResponse> Items { get; set; } = [];
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}

public sealed class PlatformSessionItemResponse
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string? TenantCode { get; set; }
    public string? TenantName { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime LastActivityAt { get; set; }
    public DateTime? EndedAt { get; set; }
    public bool IsActive { get; set; }
    public string? EndReason { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
}

public sealed class PlatformUserAuditItemResponse
{
    public string Action { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
