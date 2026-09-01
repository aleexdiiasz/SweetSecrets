using SweetSecrets.Domain.Enums;

namespace SweetSecrets.Application.Common.Tenants;

public interface IPlatformTenantAdminService
{
    Task<PlatformTenantPage> SearchAsync(PlatformTenantSearch search, CancellationToken cancellationToken = default);
    Task<PlatformTenantDetail?> GetAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task<TenantStatusChangeOutcome> ChangeStatusAsync(Guid tenantId, TenantStatus targetStatus, Guid actorUserId, string? ipAddress, string? userAgent, CancellationToken cancellationToken = default);
}
