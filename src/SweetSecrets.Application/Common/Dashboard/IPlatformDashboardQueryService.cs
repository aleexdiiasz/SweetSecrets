using SweetSecrets.Domain.Enums;

namespace SweetSecrets.Application.Common.Dashboard;

public interface IPlatformDashboardQueryService
{
    Task<PlatformDashboardSummary> GetAsync(
        TimeSpan onlineWindow,
        int recentLimit,
        CancellationToken cancellationToken = default);
}

public sealed record PlatformDashboardSummary(
    int TotalTenants,
    IReadOnlyDictionary<TenantStatus, int> TenantsByStatus,
    int TotalUsers,
    int TenantOwners,
    int PlatformAdmins,
    int BlockedUsers,
    int PendingEmailConfirmation,
    int ActiveSessions,
    int OnlineUsers,
    IReadOnlyList<PlatformRecentTenant> RecentTenants,
    IReadOnlyList<PlatformRecentUser> RecentUsers,
    IReadOnlyList<PlatformRecentEvent> RecentEvents);

public sealed record PlatformRecentTenant(
    Guid Id, string Code, string Name, TenantStatus Status, DateTime CreatedAt);

public sealed record PlatformRecentUser(
    Guid Id, string FullName, string Email, string Role, DateTime CreatedAt);

public sealed record PlatformRecentEvent(
    string Action, string Description, DateTime CreatedAt);
