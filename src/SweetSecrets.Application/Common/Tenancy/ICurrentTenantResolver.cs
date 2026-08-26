namespace SweetSecrets.Application.Common.Tenancy;

public interface ICurrentTenantResolver
{
    Task<CurrentTenantInfo> GetRequiredAsync(CancellationToken cancellationToken = default);
}