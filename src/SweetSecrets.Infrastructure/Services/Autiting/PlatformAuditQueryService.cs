using Microsoft.EntityFrameworkCore;
using SweetSecrets.Application.Common.Auditing;
using SweetSecrets.Infrastructure.Data.Master;

namespace SweetSecrets.Infrastructure.Services.Auditing;

public sealed class PlatformAuditQueryService(MasterDbContext dbContext) : IPlatformAuditQueryService
{
    public async Task<PlatformAuditPage> SearchAsync(PlatformAuditSearch search, CancellationToken cancellationToken = default)
    {
        var query = BuildFilteredQuery(search);
        var total = await query.CountAsync(cancellationToken);
        var items = await Project(query.OrderByDescending(x => x.Log.CreatedAt).ThenByDescending(x => x.Log.Id))
            .Skip((search.Page - 1) * search.PageSize).Take(search.PageSize).ToListAsync(cancellationToken);
        return new PlatformAuditPage(items, total, search.Page, search.PageSize);
    }

    public async Task<PlatformAuditDetail?> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var item = await Project(BuildBaseQuery().Where(x => x.Log.Id == id)).FirstOrDefaultAsync(cancellationToken);
        if (item is null) return null;
        var userAgent = await dbContext.PlatformAuditLogs.AsNoTracking().Where(x => x.Id == id)
            .Select(x => x.UserAgent).FirstAsync(cancellationToken);
        return new PlatformAuditDetail(item.Id, item.Action, item.Entity, item.Description, item.CreatedAt,
            item.ActorName, item.ActorEmail, item.TenantCode, item.TenantName,
            item.TargetUserName, item.TargetUserEmail, item.IpAddress, userAgent);
    }

    internal IQueryable<AuditRow> BuildFilteredQuery(PlatformAuditSearch search)
    {
        var query = BuildBaseQuery();
        var term = search.Search?.Trim();
        if (!string.IsNullOrWhiteSpace(term))
            query = query.Where(x => EF.Functions.ILike(x.Log.Action, $"%{term}%") ||
                EF.Functions.ILike(x.Log.Description, $"%{term}%") ||
                (x.Actor != null && (EF.Functions.ILike(x.Actor.FullName, $"%{term}%") ||
                    (x.Actor.Email != null && EF.Functions.ILike(x.Actor.Email, $"%{term}%")))) ||
                (x.Tenant != null && (EF.Functions.ILike(x.Tenant.Name, $"%{term}%") || EF.Functions.ILike(x.Tenant.Code, $"%{term}%"))) ||
                (x.Log.Entity == "USER" && x.Target != null && (EF.Functions.ILike(x.Target.FullName, $"%{term}%") ||
                    (x.Target.Email != null && EF.Functions.ILike(x.Target.Email, $"%{term}%")))));
        if (!string.IsNullOrWhiteSpace(search.Action)) query = query.Where(x => x.Log.Action == search.Action.Trim());
        if (search.From.HasValue) query = query.Where(x => x.Log.CreatedAt >= search.From.Value);
        if (search.ToExclusive.HasValue) query = query.Where(x => x.Log.CreatedAt < search.ToExclusive.Value);
        ApplyTextFilter(ref query, search.Tenant, true);
        ApplyActorFilter(ref query, search.Actor);
        ApplyTargetFilter(ref query, search.TargetUser);
        return query;
    }

    internal IQueryable<PlatformAuditSummary> BuildPageQuery(PlatformAuditSearch search) =>
        Project(BuildFilteredQuery(search).OrderByDescending(x => x.Log.CreatedAt).ThenByDescending(x => x.Log.Id))
            .Skip((search.Page - 1) * search.PageSize).Take(search.PageSize);

    private IQueryable<AuditRow> BuildBaseQuery() =>
        from log in dbContext.PlatformAuditLogs.AsNoTracking()
        join actorItem in dbContext.Users.AsNoTracking() on log.UserId equals actorItem.Id into actors
        from actor in actors.DefaultIfEmpty()
        join tenantItem in dbContext.Tenants.AsNoTracking() on log.TenantId equals tenantItem.Id into tenants
        from tenant in tenants.DefaultIfEmpty()
        join targetItem in dbContext.Users.AsNoTracking() on log.EntityId equals targetItem.Id.ToString() into targets
        from target in targets.DefaultIfEmpty()
        select new AuditRow { Log = log, Actor = actor, Tenant = tenant, Target = target };

    private IQueryable<PlatformAuditSummary> Project(IQueryable<AuditRow> query) => query.Select(x => new PlatformAuditSummary(
        x.Log.Id, x.Log.Action, x.Log.Entity, x.Log.Description, x.Log.CreatedAt,
        x.Actor == null ? null : x.Actor.FullName, x.Actor == null ? null : x.Actor.Email,
        x.Tenant == null ? null : x.Tenant.Code, x.Tenant == null ? null : x.Tenant.Name,
        x.Log.Entity == "USER" && x.Target != null ? x.Target.FullName : null,
        x.Log.Entity == "USER" && x.Target != null ? x.Target.Email : null,
        x.Log.IpAddress));

    private static void ApplyTextFilter(ref IQueryable<AuditRow> query, string? term, bool tenant)
    {
        term = term?.Trim();
        if (!string.IsNullOrWhiteSpace(term) && tenant)
            query = query.Where(x => x.Tenant != null && (EF.Functions.ILike(x.Tenant.Name, $"%{term}%") || EF.Functions.ILike(x.Tenant.Code, $"%{term}%")));
    }

    private static void ApplyActorFilter(ref IQueryable<AuditRow> query, string? term)
    {
        term = term?.Trim();
        if (!string.IsNullOrWhiteSpace(term))
            query = query.Where(x => x.Actor != null && (EF.Functions.ILike(x.Actor.FullName, $"%{term}%") ||
                (x.Actor.Email != null && EF.Functions.ILike(x.Actor.Email, $"%{term}%"))));
    }

    private static void ApplyTargetFilter(ref IQueryable<AuditRow> query, string? term)
    {
        term = term?.Trim();
        if (!string.IsNullOrWhiteSpace(term))
            query = query.Where(x => x.Log.Entity == "USER" && x.Target != null &&
                (EF.Functions.ILike(x.Target.FullName, $"%{term}%") ||
                 (x.Target.Email != null && EF.Functions.ILike(x.Target.Email, $"%{term}%"))));
    }

    internal sealed class AuditRow
    {
        public required Domain.Entities.Master.PlatformAuditLog Log { get; init; }
        public Identity.ApplicationUser? Actor { get; init; }
        public Domain.Entities.Master.Tenant? Tenant { get; init; }
        public Identity.ApplicationUser? Target { get; init; }
    }
}
