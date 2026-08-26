namespace SweetSecrets.Application.Common.Tenancy;

public interface ITenantDatabaseManager
{
    Task<bool> ExistsAsync(string databaseName, CancellationToken cancellationToken = default);

    Task CreateAsync(string databaseName, CancellationToken cancellationToken = default);
}