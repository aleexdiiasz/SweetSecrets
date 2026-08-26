namespace SweetSecrets.Application.Common.Auditing;

public interface IPlatformAuditService
{
    Task RegisterAsync(
        PlatformAuditEntry entry,
        CancellationToken cancellationToken = default);
}