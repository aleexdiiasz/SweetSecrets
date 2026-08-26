namespace SweetSecrets.Contracts.Admin.Tenants;

public sealed class CreateTenantOwnerRequest
{
    public Guid TenantId { get; set; }

    public string Email { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;
}