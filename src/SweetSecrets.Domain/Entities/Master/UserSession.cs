namespace SweetSecrets.Domain.Entities.Master;

public class UserSession
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public DateTime StartedAt { get; set; }

    public DateTime LastActivityAt { get; set; }

    public DateTime? EndedAt { get; set; }

    public string? IpAddress { get; set; }

    public string? UserAgent { get; set; }

    public bool IsActive { get; set; }

    public string? EndReason { get; set; }
}