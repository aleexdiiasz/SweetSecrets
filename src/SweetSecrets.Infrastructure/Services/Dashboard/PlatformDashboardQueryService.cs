using Microsoft.EntityFrameworkCore;
using SweetSecrets.Application.Common.Dashboard;
using SweetSecrets.Application.Common.Security;
using SweetSecrets.Domain.Enums;
using SweetSecrets.Infrastructure.Data.Master;

namespace SweetSecrets.Infrastructure.Services.Dashboard;

public sealed class PlatformDashboardQueryService(MasterDbContext dbContext)
    : IPlatformDashboardQueryService
{
    public async Task<PlatformDashboardSummary> GetAsync(
        TimeSpan onlineWindow,
        int recentLimit,
        CancellationToken cancellationToken = default)
    {
        var cutoff = DateTime.UtcNow.Subtract(onlineWindow);
        recentLimit = Math.Clamp(recentLimit, 1, 20);

        var tenantCounts = await dbContext.Tenants.AsNoTracking()
            .GroupBy(x => x.Status)
            .Select(group => new { Status = group.Key, Count = group.Count() })
            .ToListAsync(cancellationToken);
        var tenantsByStatus = Enum.GetValues<TenantStatus>().ToDictionary(status => status, _ => 0);
        foreach (var item in tenantCounts) tenantsByStatus[item.Status] = item.Count;

        var roleCounts = await (
            from userRole in dbContext.UserRoles.AsNoTracking()
            join role in dbContext.Roles.AsNoTracking() on userRole.RoleId equals role.Id
            group role by role.Name into roles
            select new { Role = roles.Key, Count = roles.Count() })
            .ToDictionaryAsync(x => x.Role ?? string.Empty, x => x.Count, cancellationToken);

        var totalUsers = await dbContext.Users.AsNoTracking().CountAsync(cancellationToken);
        var blockedUsers = await dbContext.Users.AsNoTracking().CountAsync(x => x.IsBlocked, cancellationToken);
        var pendingConfirmation = await dbContext.Users.AsNoTracking().CountAsync(x => !x.EmailConfirmed, cancellationToken);
        var activeSessions = await dbContext.UserSessions.AsNoTracking().CountAsync(x => x.IsActive, cancellationToken);
        var onlineUsers = await dbContext.UserSessions.AsNoTracking()
            .Where(x => x.IsActive && x.LastActivityAt >= cutoff)
            .Select(x => x.UserId).Distinct().CountAsync(cancellationToken);

        var recentTenants = await BuildRecentTenantsQuery(recentLimit).ToListAsync(cancellationToken);
        var recentUsers = await BuildRecentUsersQuery(recentLimit).ToListAsync(cancellationToken);
        var recentEvents = await BuildRecentEventsQuery(recentLimit).ToListAsync(cancellationToken);

        return new PlatformDashboardSummary(
            tenantsByStatus.Values.Sum(), tenantsByStatus, totalUsers,
            roleCounts.GetValueOrDefault(PlatformRoles.TenantOwner),
            roleCounts.GetValueOrDefault(PlatformRoles.PlatformAdmin),
            blockedUsers, pendingConfirmation, activeSessions, onlineUsers,
            recentTenants, recentUsers, recentEvents);
    }

    internal IQueryable<Guid> BuildOnlineUsersQuery(DateTime cutoff) =>
        dbContext.UserSessions.AsNoTracking()
            .Where(x => x.IsActive && x.LastActivityAt >= cutoff)
            .Select(x => x.UserId).Distinct();

    internal IQueryable<PlatformRecentTenant> BuildRecentTenantsQuery(int limit) =>
        dbContext.Tenants.AsNoTracking().OrderByDescending(x => x.CreatedAt).ThenBy(x => x.Id)
            .Take(limit).Select(x => new PlatformRecentTenant(x.Id, x.Code, x.Name, x.Status, x.CreatedAt));

    internal IQueryable<PlatformRecentUser> BuildRecentUsersQuery(int limit) =>
        (from user in dbContext.Users.AsNoTracking()
         join userRole in dbContext.UserRoles.AsNoTracking() on user.Id equals userRole.UserId
         join role in dbContext.Roles.AsNoTracking() on userRole.RoleId equals role.Id
         orderby user.CreatedAt descending, user.Id
         select new PlatformRecentUser(user.Id, user.FullName, user.Email ?? string.Empty,
             role.Name ?? string.Empty, user.CreatedAt))
            .Take(limit);

    internal IQueryable<PlatformRecentEvent> BuildRecentEventsQuery(int limit) =>
        dbContext.PlatformAuditLogs.AsNoTracking()
            .OrderByDescending(x => x.CreatedAt).ThenBy(x => x.Id).Take(limit)
            .Select(x => new PlatformRecentEvent(x.Action, x.Description, x.CreatedAt));
}
