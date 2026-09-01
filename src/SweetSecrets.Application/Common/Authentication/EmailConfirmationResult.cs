namespace SweetSecrets.Application.Common.Authentication;

public sealed record EmailConfirmationResult(bool Succeeded, string? ErrorMessage)
{
    public static EmailConfirmationResult Success() => new(true, null);
    public static EmailConfirmationResult Failed(string message) => new(false, message);
}
