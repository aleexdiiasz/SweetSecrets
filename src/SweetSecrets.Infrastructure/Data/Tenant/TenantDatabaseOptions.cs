namespace SweetSecrets.Infrastructure.Data.Tenant;

public class TenantDatabaseOptions
{
    public string AdminConnectionString { get; set; } = string.Empty;

    public string DatabasePrefix { get; set; } = "sweetsecrets_tenant_";
}