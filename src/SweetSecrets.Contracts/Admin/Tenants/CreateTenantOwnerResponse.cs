namespace SweetSecrets.Contracts.Admin.Tenants;

public sealed class CreateTenantOwnerResponse
{
    public Guid UserId { get; set; }

    public Guid TenantId { get; set; }

    public string Email { get; set; } = string.Empty;
}