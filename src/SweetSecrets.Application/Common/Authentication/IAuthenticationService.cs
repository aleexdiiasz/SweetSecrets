namespace SweetSecrets.Application.Common.Authentication;

public interface IAuthenticationService
{
    Task<AuthenticationResult> LoginAsync(
        string email,
        string password,
        bool rememberMe,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken = default);

    Task LogoutAsync(
        Guid userId,
        Guid? sessionId,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken = default);
}