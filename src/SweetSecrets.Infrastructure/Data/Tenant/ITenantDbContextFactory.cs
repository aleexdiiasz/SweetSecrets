namespace SweetSecrets.Infrastructure.Data.Tenant;

public interface ITenantDbContextFactory
{
    Task<TenantDbContext> CreateAsync(CancellationToken cancellationToken = default);
}