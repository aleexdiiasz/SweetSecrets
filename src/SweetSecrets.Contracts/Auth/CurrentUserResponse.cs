namespace SweetSecrets.Contracts.Auth;

public class CurrentUserResponse
{
    public Guid UserId { get; set; }

    public Guid? TenantId { get; set; }

    public Guid? SessionId { get; set; }

    public string Email { get; set; } = string.Empty;

    public IReadOnlyList<string> Roles { get; set; } = [];
}