namespace SweetSecrets.Application.Common.Users;

public interface IPlatformUserAdminService
{
    Task<PlatformUserChangeOutcome> BlockUserAsync(
        Guid userId,
        Guid performedByUserId,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken = default);

    Task<PlatformUserChangeOutcome> UnblockUserAsync(
        Guid userId,
        Guid performedByUserId,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken = default);

    Task<PlatformSessionRevokeOutcome> RevokeSessionAsync(
        Guid sessionId,
        Guid performedByUserId,
        Guid? performedFromSessionId,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken = default);
}
