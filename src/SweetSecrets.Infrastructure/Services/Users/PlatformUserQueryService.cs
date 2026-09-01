using Microsoft.EntityFrameworkCore;
using SweetSecrets.Application.Common.Users;
using SweetSecrets.Infrastructure.Data.Master;

namespace SweetSecrets.Infrastructure.Services.Users;

public sealed class PlatformUserQueryService(MasterDbContext dbContext) : IPlatformUserQueryService
{
    public async Task<PlatformUserPage> SearchAsync(PlatformUserSearch search, TimeSpan onlineWindow, CancellationToken cancellationToken = default)
    {
        var cutoff = DateTime.UtcNow.Subtract(onlineWindow);
        var query = BuildUserQuery(search.Search, search.Role);
        if (search.IsBlocked.HasValue)
            query = query.Where(x => x.User.IsBlocked == search.IsBlocked.Value);
        if (search.IsOnline.HasValue)
            query = query.Where(x => dbContext.UserSessions.Any(s => s.UserId == x.User.Id && s.IsActive && s.LastActivityAt >= cutoff) == search.IsOnline.Value);

        var total = await query.CountAsync(cancellationToken);
        var rows = await query.OrderBy(x => x.User.FullName).Skip((search.Page - 1) * search.PageSize).Take(search.PageSize)
            .Select(x => new { x.User.Id, x.User.Email, x.User.FullName, Role = x.Role.Name, x.User.TenantId,
                TenantCode = x.Tenant == null ? null : x.Tenant.Code, TenantName = x.Tenant == null ? null : x.Tenant.Name,
                x.User.IsActive, x.User.IsBlocked, x.User.EmailConfirmed,
                IsOnline = dbContext.UserSessions.Any(s => s.UserId == x.User.Id && s.IsActive && s.LastActivityAt >= cutoff),
                x.User.LastLoginAt, x.User.LastActivityAt, x.User.CreatedAt })
            .ToListAsync(cancellationToken);

        return new PlatformUserPage(rows.Select(x => new PlatformUserSummary(x.Id, x.Email ?? string.Empty, x.FullName,
            x.Role ?? string.Empty, x.TenantId, x.TenantCode, x.TenantName, x.IsActive, x.IsBlocked,
            x.EmailConfirmed, x.IsOnline, x.LastLoginAt, x.LastActivityAt, x.CreatedAt)).ToList(), total, search.Page, search.PageSize);
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

    private IQueryable<UserRow> BuildUserQuery(string? search, string? role)
    {
        var query = from user in dbContext.Users.AsNoTracking()
                    join userRole in dbContext.UserRoles.AsNoTracking() on user.Id equals userRole.UserId
                    join roleItem in dbContext.Roles.AsNoTracking() on userRole.RoleId equals roleItem.Id
                    join tenantItem in dbContext.Tenants.AsNoTracking() on user.TenantId equals tenantItem.Id into tenants
                    from tenant in tenants.DefaultIfEmpty()
                    select new UserRow(user, roleItem, tenant);
        var term = search?.Trim();
        if (!string.IsNullOrWhiteSpace(term))
        {
            if (Guid.TryParse(term, out var userId)) query = query.Where(x => x.User.Id == userId);
            else query = query.Where(x => EF.Functions.ILike(x.User.FullName, $"%{term}%") ||
                (x.User.Email != null && EF.Functions.ILike(x.User.Email, $"%{term}%")) ||
                (x.Tenant != null && (EF.Functions.ILike(x.Tenant.Name, $"%{term}%") || EF.Functions.ILike(x.Tenant.Code, $"%{term}%"))));
        }
        if (!string.IsNullOrWhiteSpace(role)) query = query.Where(x => x.Role.Name == role);
        return query;
    }

    private sealed record UserRow(Identity.ApplicationUser User, Microsoft.AspNetCore.Identity.IdentityRole<Guid> Role,
        Domain.Entities.Master.Tenant? Tenant);
}
