using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using SweetSecrets.Infrastructure.Data.Tenant;

namespace SweetSecrets.Api.Infrastructure;

public class TenantDbContextFactory
    : IDesignTimeDbContextFactory<TenantDbContext>
{
    public TenantDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddUserSecrets<TenantDbContextFactory>(optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString =
            configuration.GetConnectionString("TenantDatabaseTemplate")
            ?? throw new InvalidOperationException(
                "Connection string 'TenantDatabaseTemplate' was not configured.");

        var optionsBuilder =
            new DbContextOptionsBuilder<TenantDbContext>();

        optionsBuilder.UseNpgsql(connectionString);

        return new TenantDbContext(optionsBuilder.Options);
    }
}