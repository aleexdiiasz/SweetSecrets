namespace SweetSecrets.Application.Common.Users;

public class PlatformUserSummary
{
    public Guid Id { get; set; }

    public Guid? TenantId { get; set; }

    public string Email { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;

    public bool IsActive { get; set; }

    public bool IsBlocked { get; set; }

    public bool IsOnline { get; set; }

    public DateTime? LastLoginAt { get; set; }

    public DateTime? LastActivityAt { get; set; }

    public DateTime CreatedAt { get; set; }
}