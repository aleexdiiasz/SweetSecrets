namespace SweetSecrets.Application.Common.Tenancy;

public interface ITenantSeedService
{
    Task SeedAsync(
        string connectionString,
        CancellationToken cancellationToken = default);
}