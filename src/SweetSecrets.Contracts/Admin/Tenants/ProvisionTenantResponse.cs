namespace SweetSecrets.Contracts.Admin.Tenants;

public class ProvisionTenantResponse
{
    public Guid TenantId { get; set; }

    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string DatabaseName { get; set; } = string.Empty;
}