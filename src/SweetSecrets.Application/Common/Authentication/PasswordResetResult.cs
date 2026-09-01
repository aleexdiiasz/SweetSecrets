namespace SweetSecrets.Application.Common.Authentication;

public sealed record PasswordResetResult(bool Succeeded, string? ErrorMessage)
{
    public static PasswordResetResult Success() => new(true, null);

    public static PasswordResetResult Failed(string message) => new(false, message);
}
