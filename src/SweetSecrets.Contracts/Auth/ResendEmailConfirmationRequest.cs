namespace SweetSecrets.Contracts.Auth;

public sealed class ResendEmailConfirmationRequest
{
    public string Email { get; set; } = string.Empty;
}
