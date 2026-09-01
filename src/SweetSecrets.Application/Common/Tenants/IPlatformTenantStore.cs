using SweetSecrets.Domain.Enums;

namespace SweetSecrets.Application.Common.Tenants;

public interface IPlatformTenantStore
{
    Task<PlatformTenantPage> SearchAsync(PlatformTenantSearch search, CancellationToken cancellationToken = default);
    Task<PlatformTenantDetail?> GetAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task<bool> TryChangeStatusAsync(Guid tenantId, TenantStatus expectedStatus, TenantStatus newStatus, DateTime updatedAt, CancellationToken cancellationToken = default);
}
