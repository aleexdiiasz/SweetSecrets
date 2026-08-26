using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Npgsql;
using SweetSecrets.Application.Common.Tenancy;

namespace SweetSecrets.Infrastructure.Data.Tenant;

public sealed class CurrentTenantDbContextFactory : ITenantDbContextFactory
{
    private readonly ICurrentTenantResolver _tenantResolver;
    private readonly TenantDatabaseOptions _databaseOptions;

    public CurrentTenantDbContextFactory(ICurrentTenantResolver tenantResolver, IOptions<TenantDatabaseOptions> databaseOptions)
    {
        _tenantResolver = tenantResolver;
        _databaseOptions = databaseOptions.Value;

        if (string.IsNullOrWhiteSpace(_databaseOptions.AdminConnectionString))
        {
            throw new InvalidOperationException(
                "La conexión administrativa PostgreSQL no está configurada.");
        }

        if (string.IsNullOrWhiteSpace(_databaseOptions.DatabasePrefix))
        {
            throw new InvalidOperationException(
                "El prefijo de bases tenant no está configurado.");
        }
    }

    public async Task<TenantDbContext> CreateAsync(CancellationToken cancellationToken = default)
    {
        var tenant =
            await _tenantResolver.GetRequiredAsync(
                cancellationToken);

        ValidateDatabaseName(tenant.DatabaseName);

        var connectionStringBuilder =
            new NpgsqlConnectionStringBuilder(
                _databaseOptions.AdminConnectionString)
            {
                Database = tenant.DatabaseName
            };

        var optionsBuilder =
            new DbContextOptionsBuilder<TenantDbContext>();

        optionsBuilder.UseNpgsql(connectionStringBuilder.ConnectionString);

        return new TenantDbContext(optionsBuilder.Options);
    }

    private void ValidateDatabaseName(string databaseName)
    {
        var pattern =
            $"^{Regex.Escape(_databaseOptions.DatabasePrefix)}\\d{{6}}$";

        if (!Regex.IsMatch(
                databaseName,
                pattern,
                RegexOptions.CultureInvariant))
        {
            throw new InvalidOperationException(
                "El nombre de la base tenant no es válido.");
        }
    }
}