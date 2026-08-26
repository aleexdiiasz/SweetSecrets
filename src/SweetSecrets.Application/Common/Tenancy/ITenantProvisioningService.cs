namespace SweetSecrets.Application.Common.Tenancy;

public interface ITenantProvisioningService
{
    Task<TenantProvisioningResult> ProvisionAsync(
        string tenantName,
        CancellationToken cancellationToken = default);
}