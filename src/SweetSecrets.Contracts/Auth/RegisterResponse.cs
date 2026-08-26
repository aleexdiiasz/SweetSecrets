namespace SweetSecrets.Contracts.Auth;

public sealed class RegisterResponse
{
    public Guid UserId { get; set; }

    public Guid TenantId { get; set; }

    public string TenantCode { get; set; } = string.Empty;

    public string BusinessName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;
}