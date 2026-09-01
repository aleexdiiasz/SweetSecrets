namespace SweetSecrets.Application.Common.Authentication;

public interface IPasswordResetNotificationService
{
    Task SendAsync(
        string email,
        Uri resetUri,
        CancellationToken cancellationToken = default);
}
