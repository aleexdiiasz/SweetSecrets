namespace SweetSecrets.Application.Common.Sessions;

public interface IUserSessionService
{
    Task<Guid> StartSessionAsync(
        Guid userId,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken = default);

    Task UpdateActivityAsync(
        Guid sessionId,
        CancellationToken cancellationToken = default);

    Task<bool> ValidateAndUpdateActivityAsync(
        Guid sessionId,
        Guid userId,
        CancellationToken cancellationToken = default);

    Task EndSessionAsync(
        Guid sessionId,
        string endReason,
        CancellationToken cancellationToken = default);

    Task EndAllUserSessionsAsync(
        Guid userId,
        string endReason,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<UserSessionInfo>> GetActiveSessionsAsync(
        TimeSpan activityWindow,
        CancellationToken cancellationToken = default);
}
