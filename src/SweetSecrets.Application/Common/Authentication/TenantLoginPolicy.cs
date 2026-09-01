using SweetSecrets.Application.Common.Security;
using SweetSecrets.Domain.Enums;

namespace SweetSecrets.Application.Common.Authentication;

public sealed class TenantLoginPolicy(ITenantStatusReader tenantStatusReader)
    : ITenantLoginPolicy
{
    public async Task<TenantLoginDecision> EvaluateAsync(
        Guid? tenantId,
        IReadOnlyCollection<string> roles,
        CancellationToken cancellationToken = default)
    {
        if (!roles.Contains(PlatformRoles.TenantOwner, StringComparer.Ordinal))
            return TenantLoginDecision.Allowed;

        if (!tenantId.HasValue)
            return TenantLoginDecision.Unavailable;

        var status = await tenantStatusReader.GetStatusAsync(
            tenantId.Value,
            cancellationToken);

        return status switch
        {
            TenantStatus.Active => TenantLoginDecision.Allowed,
            TenantStatus.Suspended => TenantLoginDecision.Suspended,
            _ => TenantLoginDecision.Unavailable
        };
    }
}
