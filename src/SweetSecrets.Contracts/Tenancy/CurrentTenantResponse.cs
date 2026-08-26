namespace SweetSecrets.Contracts.Tenancy;

public sealed class CurrentTenantResponse
{
    public Guid TenantId { get; set; }

    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;
}