namespace SweetSecrets.Contracts.Auth;

public sealed class AccountResponse
{
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}
