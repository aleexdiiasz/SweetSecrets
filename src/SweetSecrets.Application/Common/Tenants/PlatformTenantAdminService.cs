using SweetSecrets.Application.Common.Auditing;
using SweetSecrets.Domain.Enums;

namespace SweetSecrets.Application.Common.Tenants;

public sealed class PlatformTenantAdminService : IPlatformTenantAdminService
{
    private readonly IPlatformTenantStore _store;
    private readonly IPlatformAuditService _auditService;

    public PlatformTenantAdminService(IPlatformTenantStore store, IPlatformAuditService auditService)
    {
        _store = store;
        _auditService = auditService;
    }

    public Task<PlatformTenantPage> SearchAsync(PlatformTenantSearch search, CancellationToken cancellationToken = default) =>
        _store.SearchAsync(search, cancellationToken);

    public Task<PlatformTenantDetail?> GetAsync(Guid tenantId, CancellationToken cancellationToken = default) =>
        _store.GetAsync(tenantId, cancellationToken);

    public async Task<TenantStatusChangeOutcome> ChangeStatusAsync(
        Guid tenantId,
        TenantStatus targetStatus,
        Guid actorUserId,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken = default)
    {
        var tenant = await _store.GetAsync(tenantId, cancellationToken);
        if (tenant is null)
            return TenantStatusChangeOutcome.NotFound;

        if (!IsAllowed(tenant.Status, targetStatus))
            return TenantStatusChangeOutcome.InvalidTransition;

        var changed = await _store.TryChangeStatusAsync(
            tenantId, tenant.Status, targetStatus, DateTime.UtcNow, cancellationToken);
        if (!changed)
            return TenantStatusChangeOutcome.StateChanged;

        var action = targetStatus == TenantStatus.Suspended
            ? "TENANT_SUSPENDED"
            : "TENANT_ACTIVATED";

        await _auditService.RegisterAsync(new PlatformAuditEntry
        {
            UserId = actorUserId,
            TenantId = tenantId,
            Action = action,
            Entity = "TENANT",
            EntityId = tenantId.ToString(),
            Description = $"Tenant {tenant.Code} cambió de {tenant.Status} a {targetStatus}.",
            OldValues = tenant.Status.ToString(),
            NewValues = targetStatus.ToString(),
            IpAddress = ipAddress,
            UserAgent = userAgent
        }, cancellationToken);

        return TenantStatusChangeOutcome.Success;
    }

    public static bool IsAllowed(TenantStatus current, TenantStatus target) =>
        (current, target) is
            (TenantStatus.Active, TenantStatus.Suspended) or
            (TenantStatus.Suspended, TenantStatus.Active);
}
