namespace SweetSecrets.Application.Common.Authentication;

public interface IPasswordRecoveryService
{
    Task RequestResetAsync(string email, CancellationToken cancellationToken = default);

    Task<PasswordResetResult> ResetPasswordAsync(
        string email,
        string token,
        string newPassword,
        CancellationToken cancellationToken = default);
}
