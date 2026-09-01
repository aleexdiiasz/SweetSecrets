namespace SweetSecrets.Contracts.Auth;

public class LoginResponse
{
    public Guid UserId { get; set; }

    public Guid? TenantId { get; set; }

    public IReadOnlyList<string> Roles { get; set; } = [];

    public string Message { get; set; } = string.Empty;
}
