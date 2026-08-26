namespace SweetSecrets.Application.Common.Tenancy;

public interface ITenantIdentifierGenerator
{
    Task<TenantIdentifier> GenerateAsync(
        CancellationToken cancellationToken = default);
}