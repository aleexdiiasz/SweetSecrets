using Microsoft.AspNetCore.Identity;

namespace SweetSecrets.Infrastructure.Identity;

public class ApplicationUser : IdentityUser<Guid>
{
    public Guid? TenantId { get; set; }

    public string FullName { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public bool IsBlocked { get; set; }

    public DateTime? LastLoginAt { get; set; }

    public DateTime? LastActivityAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}