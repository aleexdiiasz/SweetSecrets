namespace SweetSecrets.Application.Common.Auditing;

public class PlatformAuditEntry
{
    public Guid? UserId { get; set; }

    public Guid? TenantId { get; set; }

    public string Action { get; set; } = string.Empty;

    public string Entity { get; set; } = string.Empty;

    public string? EntityId { get; set; }

    public string Description { get; set; } = string.Empty;

    public string? OldValues { get; set; }

    public string? NewValues { get; set; }

    public string? IpAddress { get; set; }

    public string? UserAgent { get; set; }
}