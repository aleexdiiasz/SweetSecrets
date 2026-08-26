namespace SweetSecrets.Application.Common.Tenancy;

public interface ITenantRegistryService
{
    Task<TenantRegistrationResult> CreateProvisioningAsync(
        string name,
        CancellationToken cancellationToken = default);

    Task MarkActiveAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default);

    Task MarkFailedAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default);
}