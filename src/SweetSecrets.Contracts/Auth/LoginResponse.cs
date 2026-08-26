namespace SweetSecrets.Contracts.Auth;

public class LoginResponse
{
    public Guid UserId { get; set; }

    public Guid? TenantId { get; set; }

    public string Message { get; set; } = string.Empty;
}