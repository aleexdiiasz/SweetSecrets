namespace SweetSecrets.Application.Common.Users;

public interface IPlatformUserQueryService
{
    Task<IReadOnlyList<PlatformUserSummary>> GetUsersAsync(
        TimeSpan onlineWindow,
        CancellationToken cancellationToken = default);
}