using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using SweetSecrets.Infrastructure.Data.Master;

namespace SweetSecrets.Api.Infrastructure;

public class MasterDbContextFactory
    : IDesignTimeDbContextFactory<MasterDbContext>
{
    public MasterDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddUserSecrets<MasterDbContextFactory>(optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString =
            configuration.GetConnectionString("MasterDatabase")
            ?? throw new InvalidOperationException(
                "Connection string 'MasterDatabase' was not configured.");

        var optionsBuilder = new DbContextOptionsBuilder<MasterDbContext>();

        optionsBuilder.UseNpgsql(connectionString);

        return new MasterDbContext(optionsBuilder.Options);
    }
}