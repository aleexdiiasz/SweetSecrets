namespace SweetSecrets.Application.Common.Users;

public sealed record PlatformUserSearch(
    string? Search,
    string? Role,
    bool? IsBlocked,
    bool? IsOnline,
    int Page,
    int PageSize);

public sealed record PlatformUserPage(
    IReadOnlyList<PlatformUserSummary> Items,
    int TotalCount,
    int Page,
    int PageSize);

public sealed record PlatformUserSummary(
    Guid Id,
    string Email,
    string FullName,
    string Role,
    Guid? TenantId,
    string? TenantCode,
    string? TenantName,
    bool IsActive,
    bool IsBlocked,
    bool EmailConfirmed,
    bool IsOnline,
    DateTime? LastLoginAt,
    DateTime? LastActivityAt,
    DateTime CreatedAt);

public sealed record PlatformUserDetail(
    PlatformUserSummary User,
    IReadOnlyList<PlatformUserSessionSummary> RecentSessions,
    IReadOnlyList<PlatformUserAuditItem> RecentAudit);

public sealed record PlatformUserSessionSearch(
    string? Search,
    bool? IsActive,
    int Page,
    int PageSize,
    Guid? UserId = null);

public sealed record PlatformUserSessionPage(
    IReadOnlyList<PlatformUserSessionSummary> Items,
    int TotalCount,
    int Page,
    int PageSize);

public sealed record PlatformUserSessionSummary(
    Guid Id,
    Guid UserId,
    string UserName,
    string Email,
    string Role,
    string? TenantCode,
    string? TenantName,
    DateTime StartedAt,
    DateTime LastActivityAt,
    DateTime? EndedAt,
    bool IsActive,
    string? EndReason,
    string? IpAddress,
    string? UserAgent);

public sealed record PlatformUserAuditItem(
    string Action,
    string Description,
    DateTime CreatedAt);

public enum PlatformUserChangeOutcome
{
    Success,
    NotFound,
    AlreadyInState,
    Forbidden
}

public enum PlatformSessionRevokeOutcome
{
    Success,
    NotFound,
    AlreadyEnded,
    Forbidden
}
