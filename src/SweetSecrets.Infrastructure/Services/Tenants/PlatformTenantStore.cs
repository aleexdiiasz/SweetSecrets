using Microsoft.EntityFrameworkCore;
using SweetSecrets.Application.Common.Security;
using SweetSecrets.Application.Common.Tenants;
using SweetSecrets.Domain.Enums;
using SweetSecrets.Infrastructure.Data.Master;

namespace SweetSecrets.Infrastructure.Services.Tenants;

public sealed class PlatformTenantStore : IPlatformTenantStore
{
    private readonly MasterDbContext _dbContext;

    public PlatformTenantStore(MasterDbContext dbContext) => _dbContext = dbContext;

    public async Task<PlatformTenantPage> SearchAsync(PlatformTenantSearch search, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Tenants.AsNoTracking();
        var term = search.Search?.Trim();
        if (!string.IsNullOrWhiteSpace(term))
        {
            query = query.Where(tenant =>
                EF.Functions.ILike(tenant.Name, $"%{term}%") ||
                EF.Functions.ILike(tenant.Code, $"%{term}%") ||
                _dbContext.Users.Any(user => user.TenantId == tenant.Id &&
                    (EF.Functions.ILike(user.FullName, $"%{term}%") ||
                     (user.Email != null && EF.Functions.ILike(user.Email, $"%{term}%")))));
        }

        if (search.Status.HasValue)
            query = query.Where(tenant => tenant.Status == search.Status.Value);

        var total = await query.CountAsync(cancellationToken);
        var tenants = await query.OrderBy(tenant => tenant.Name)
            .Skip((search.Page - 1) * search.PageSize).Take(search.PageSize)
            .Select(tenant => new { tenant.Id, tenant.Code, tenant.Name, tenant.Status, tenant.CreatedAt, tenant.UpdatedAt })
            .ToListAsync(cancellationToken);

        var owners = await GetOwnersAsync(tenants.Select(tenant => tenant.Id).ToList(), cancellationToken);
        var items = tenants.Select(tenant =>
        {
            owners.TryGetValue(tenant.Id, out var owner);
            return new PlatformTenantSummary(tenant.Id, tenant.Code, tenant.Name, tenant.Status,
                tenant.CreatedAt, tenant.UpdatedAt, owner?.FullName, owner?.Email);
        }).ToList();

        return new PlatformTenantPage(items, total, search.Page, search.PageSize);
    }

    public async Task<PlatformTenantDetail?> GetAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        var tenant = await _dbContext.Tenants.AsNoTracking()
            .Where(item => item.Id == tenantId)
            .Select(item => new { item.Id, item.Code, item.Name, item.Status, item.CreatedAt, item.UpdatedAt })
            .FirstOrDefaultAsync(cancellationToken);
        if (tenant is null)
            return null;

        var owners = await GetOwnersAsync([tenantId], cancellationToken);
        owners.TryGetValue(tenantId, out var owner);
        var audit = await _dbContext.PlatformAuditLogs.AsNoTracking()
            .Where(item => item.TenantId == tenantId)
            .OrderByDescending(item => item.CreatedAt).Take(10)
            .Select(item => new PlatformTenantAuditItem(item.Action, item.Description, item.CreatedAt))
            .ToListAsync(cancellationToken);

        return new PlatformTenantDetail(tenant.Id, tenant.Code, tenant.Name, tenant.Status,
            tenant.CreatedAt, tenant.UpdatedAt, owner?.Id, owner?.FullName, owner?.Email,
            owner?.LastLoginAt, owner?.LastActivityAt, audit);
    }

    public async Task<bool> TryChangeStatusAsync(Guid tenantId, TenantStatus expectedStatus, TenantStatus newStatus, DateTime updatedAt, CancellationToken cancellationToken = default) =>
        await _dbContext.Tenants
            .Where(tenant => tenant.Id == tenantId && tenant.Status == expectedStatus)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(tenant => tenant.Status, newStatus)
                .SetProperty(tenant => tenant.UpdatedAt, updatedAt), cancellationToken) == 1;

    private async Task<Dictionary<Guid, OwnerProjection>> GetOwnersAsync(IReadOnlyCollection<Guid> tenantIds, CancellationToken cancellationToken)
    {
        var owners = await (
            from user in _dbContext.Users.AsNoTracking()
            join userRole in _dbContext.UserRoles on user.Id equals userRole.UserId
            join role in _dbContext.Roles on userRole.RoleId equals role.Id
            where user.TenantId.HasValue && tenantIds.Contains(user.TenantId.Value) && role.Name == PlatformRoles.TenantOwner
            orderby user.CreatedAt
            select new OwnerProjection(user.Id, user.TenantId!.Value, user.FullName, user.Email,
                user.LastLoginAt, user.LastActivityAt))
            .ToListAsync(cancellationToken);

        return owners.GroupBy(owner => owner.TenantId)
            .ToDictionary(group => group.Key, group => group.First());
    }

    private sealed record OwnerProjection(Guid Id, Guid TenantId, string FullName, string? Email,
        DateTime? LastLoginAt, DateTime? LastActivityAt);
}
