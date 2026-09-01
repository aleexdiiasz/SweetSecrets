namespace SweetSecrets.Application.Common.Users;

public interface IPlatformUserQueryService
{
    Task<PlatformUserPage> SearchAsync(
        PlatformUserSearch search,
        TimeSpan onlineWindow,
        CancellationToken cancellationToken = default);

    Task<PlatformUserDetail?> GetAsync(
        Guid userId,
        TimeSpan onlineWindow,
        CancellationToken cancellationToken = default);

    Task<PlatformUserSessionPage> SearchSessionsAsync(
        PlatformUserSessionSearch search,
        CancellationToken cancellationToken = default);
}
