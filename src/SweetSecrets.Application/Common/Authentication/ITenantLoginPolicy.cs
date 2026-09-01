using SweetSecrets.Domain.Enums;

namespace SweetSecrets.Application.Common.Authentication;

public interface ITenantStatusReader
{
    Task<TenantStatus?> GetStatusAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default);
}

public interface ITenantLoginPolicy
{
    Task<TenantLoginDecision> EvaluateAsync(
        Guid? tenantId,
        IReadOnlyCollection<string> roles,
        CancellationToken cancellationToken = default);
}

public enum TenantLoginDecision
{
    Allowed,
    Suspended,
    Unavailable
}
