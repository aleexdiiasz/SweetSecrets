namespace SweetSecrets.Application.Common.Users;

public interface IPlatformUserAdminService
{
    Task BlockUserAsync(
        Guid userId,
        Guid performedByUserId,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken = default);

    Task UnblockUserAsync(
        Guid userId,
        Guid performedByUserId,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken = default);
}