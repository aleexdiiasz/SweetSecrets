namespace SweetSecrets.Application.Common.Tenancy;

public interface ITenantUserProvisioningService
{
    Task<Guid> CreateOwnerAsync(
        Guid tenantId,
        string email,
        string fullName,
        string password,
        CancellationToken cancellationToken = default);
}