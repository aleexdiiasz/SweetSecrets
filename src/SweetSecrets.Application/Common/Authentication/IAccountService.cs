namespace SweetSecrets.Application.Common.Authentication;

public interface IAccountService
{
    Task<AccountInfo?> GetAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<ChangePasswordResult> ChangePasswordAsync(
        Guid userId,
        string currentPassword,
        string newPassword,
        CancellationToken cancellationToken = default);
}
