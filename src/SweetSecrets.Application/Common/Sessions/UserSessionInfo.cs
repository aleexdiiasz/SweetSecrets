namespace SweetSecrets.Application.Common.Sessions;

public class UserSessionInfo
{
    public Guid SessionId { get; set; }

    public Guid UserId { get; set; }

    public DateTime StartedAt { get; set; }

    public DateTime LastActivityAt { get; set; }

    public string? IpAddress { get; set; }

    public string? UserAgent { get; set; }
}