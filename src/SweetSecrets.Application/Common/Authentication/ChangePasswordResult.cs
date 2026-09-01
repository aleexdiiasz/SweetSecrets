namespace SweetSecrets.Application.Common.Authentication;

public sealed record ChangePasswordResult(bool Succeeded, string? ErrorMessage)
{
    public static ChangePasswordResult Success() => new(true, null);

    public static ChangePasswordResult Failed(string message) => new(false, message);
}
