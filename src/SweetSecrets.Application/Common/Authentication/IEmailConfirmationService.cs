namespace SweetSecrets.Application.Common.Authentication;

public interface IEmailConfirmationService
{
    Task SendForUserAsync(Guid userId, CancellationToken cancellationToken = default);

    Task RequestResendAsync(string email, CancellationToken cancellationToken = default);

    Task<EmailConfirmationResult> ConfirmAsync(
        string email,
        string token,
        CancellationToken cancellationToken = default);
}
