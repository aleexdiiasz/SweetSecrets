using Microsoft.EntityFrameworkCore;
using SweetSecrets.Application.Common.Users;
using SweetSecrets.Infrastructure.Data.Master;

namespace SweetSecrets.Infrastructure.Services.Users;

public sealed class PlatformUserQueryService(MasterDbContext dbContext) : IPlatformUserQueryService
{
    public async Task<PlatformUserPage> SearchAsync(PlatformUserSearch search, TimeSpan onlineWindow, CancellationToken cancellationToken = default)
    {
        var cutoff = DateTime.UtcNow.Subtract(onlineWindow);
        var total = await BuildSearchQuery(search, cutoff, applyPagination: false)
            .CountAsync(cancellationToken);
        var rows = await BuildSearchQuery(search, cutoff, applyPagination: true)
            .ToListAsync(cancellationToken);

        return new PlatformUserPage(rows, total, search.Page, search.PageSize);
    }

    public async Task<PlatformUserDetail?> GetAsync(Guid userId, TimeSpan onlineWindow, CancellationToken cancellationToken = default)
    {
        var page = await SearchAsync(new PlatformUserSearch(userId.ToString(), null, null, null, 1, 1), onlineWindow, cancellationToken);
        var user = page.Items.SingleOrDefault();
        if (user is null) return null;
        var sessions = (await SearchSessionsAsync(new PlatformUserSessionSearch(null, null, 1, 10, userId), cancellationToken)).Items;
        var audit = await dbContext.PlatformAuditLogs.AsNoTracking().Where(x => x.Entity == "USER" && x.EntityId == userId.ToString())
            .OrderByDescending(x => x.CreatedAt).Take(10)
            .Select(x => new PlatformUserAuditItem(x.Action, x.Description, x.CreatedAt)).ToListAsync(cancellationToken);
        return new PlatformUserDetail(user, sessions, audit);
    }

    public async Task<PlatformUserSessionPage> SearchSessionsAsync(PlatformUserSessionSearch search, CancellationToken cancellationToken = default)
    {
        var query = from session in dbContext.UserSessions.AsNoTracking()
                    join user in dbContext.Users.AsNoTracking() on session.UserId equals user.Id
                    join userRole in dbContext.UserRoles.AsNoTracking() on user.Id equals userRole.UserId
                    join role in dbContext.Roles.AsNoTracking() on userRole.RoleId equals role.Id
                    join tenantItem in dbContext.Tenants.AsNoTracking() on user.TenantId equals tenantItem.Id into tenants
                    from tenant in tenants.DefaultIfEmpty()
                    select new { Session = session, User = user, Role = role, Tenant = tenant };
        if (search.UserId.HasValue) query = query.Where(x => x.User.Id == search.UserId.Value);
        if (search.IsActive.HasValue) query = query.Where(x => x.Session.IsActive == search.IsActive.Value);
        var term = search.Search?.Trim();
        if (!string.IsNullOrWhiteSpace(term)) query = query.Where(x => EF.Functions.ILike(x.User.FullName, $"%{term}%") ||
            (x.User.Email != null && EF.Functions.ILike(x.User.Email, $"%{term}%")) ||
            (x.Tenant != null && (EF.Functions.ILike(x.Tenant.Name, $"%{term}%") || EF.Functions.ILike(x.Tenant.Code, $"%{term}%"))));

        var total = await query.CountAsync(cancellationToken);
        var rows = await query.OrderByDescending(x => x.Session.LastActivityAt).Skip((search.Page - 1) * search.PageSize).Take(search.PageSize)
            .Select(x => new PlatformUserSessionSummary(x.Session.Id, x.User.Id, x.User.FullName, x.User.Email ?? string.Empty,
                x.Role.Name ?? string.Empty, x.Tenant == null ? null : x.Tenant.Code, x.Tenant == null ? null : x.Tenant.Name,
                x.Session.StartedAt, x.Session.LastActivityAt, x.Session.EndedAt, x.Session.IsActive, x.Session.EndReason,
                x.Session.IpAddress, x.Session.UserAgent)).ToListAsync(cancellationToken);
        return new PlatformUserSessionPage(rows, total, search.Page, search.PageSize);
    }

    internal IQueryable<PlatformUserSummary> BuildSearchQuery(
        PlatformUserSearch search,
        DateTime cutoff,
        bool applyPagination)
    {
        var query = from user in dbContext.Users.AsNoTracking()
                    join userRole in dbContext.UserRoles.AsNoTracking() on user.Id equals userRole.UserId
                    join roleItem in dbContext.Roles.AsNoTracking() on userRole.RoleId equals roleItem.Id
                    join tenantItem in dbContext.Tenants.AsNoTracking() on user.TenantId equals tenantItem.Id into tenants
                    from tenant in tenants.DefaultIfEmpty()
                    select new { User = user, Role = roleItem, Tenant = tenant };
        var term = search.Search?.Trim();
        if (!string.IsNullOrWhiteSpace(term))
        {
            if (Guid.TryParse(term, out var userId)) query = query.Where(x => x.User.Id == userId);
            else query = query.Where(x => EF.Functions.ILike(x.User.FullName, $"%{term}%") ||
                (x.User.Email != null && EF.Functions.ILike(x.User.Email, $"%{term}%")) ||
                (x.Tenant != null && (EF.Functions.ILike(x.Tenant.Name, $"%{term}%") || EF.Functions.ILike(x.Tenant.Code, $"%{term}%"))));
        }
        if (!string.IsNullOrWhiteSpace(search.Role))
            query = query.Where(x => x.Role.Name == search.Role);
        if (search.IsBlocked.HasValue)
            query = query.Where(x => x.User.IsBlocked == search.IsBlocked.Value);
        if (search.IsOnline.HasValue)
            query = query.Where(x => dbContext.UserSessions.Any(s => s.UserId == x.User.Id && s.IsActive && s.LastActivityAt >= cutoff) == search.IsOnline.Value);

        var ordered = query.OrderBy(x => x.User.FullName).ThenBy(x => x.User.Id);
        var selected = applyPagination
            ? ordered.Skip((search.Page - 1) * search.PageSize).Take(search.PageSize)
            : ordered;

        return selected.Select(x => new PlatformUserSummary(
            x.User.Id, x.User.Email ?? string.Empty, x.User.FullName, x.Role.Name ?? string.Empty,
            x.User.TenantId, x.Tenant == null ? null : x.Tenant.Code,
            x.Tenant == null ? null : x.Tenant.Name, x.User.IsActive, x.User.IsBlocked,
            x.User.EmailConfirmed,
            dbContext.UserSessions.Any(s => s.UserId == x.User.Id && s.IsActive && s.LastActivityAt >= cutoff),
            x.User.LastLoginAt, x.User.LastActivityAt, x.User.CreatedAt));
    }
}
