using SweetSecrets.Domain.Enums;

namespace SweetSecrets.Domain.Entities.Master;

public class Tenant
{
    public Guid Id { get; set; }

    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string DatabaseName { get; set; } = string.Empty;

    public TenantStatus Status { get; set; } = TenantStatus.Provisioning;

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }
}