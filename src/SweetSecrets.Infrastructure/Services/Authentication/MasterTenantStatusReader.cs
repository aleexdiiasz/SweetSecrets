using Microsoft.EntityFrameworkCore;
using SweetSecrets.Application.Common.Authentication;
using SweetSecrets.Domain.Enums;
using SweetSecrets.Infrastructure.Data.Master;

namespace SweetSecrets.Infrastructure.Services.Authentication;

public sealed class MasterTenantStatusReader(MasterDbContext masterDbContext)
    : ITenantStatusReader
{
    public Task<TenantStatus?> GetStatusAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default) =>
        masterDbContext.Tenants
            .AsNoTracking()
            .Where(x => x.Id == tenantId)
            .Select(x => (TenantStatus?)x.Status)
            .SingleOrDefaultAsync(cancellationToken);
}
