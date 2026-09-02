namespace SweetSecrets.Application.Common.Auditing;

public interface IPlatformAuditQueryService
{
    Task<PlatformAuditPage> SearchAsync(PlatformAuditSearch search, CancellationToken cancellationToken = default);
    Task<PlatformAuditDetail?> GetAsync(Guid id, CancellationToken cancellationToken = default);
}

public sealed record PlatformAuditSearch(
    string? Search, string? Action, DateTime? From, DateTime? ToExclusive,
    string? Tenant, string? Actor, string? TargetUser, int Page, int PageSize);

public sealed record PlatformAuditPage(
    IReadOnlyList<PlatformAuditSummary> Items, int TotalCount, int Page, int PageSize);

public record PlatformAuditSummary(
    Guid Id, string Action, string Entity, string Description, DateTime CreatedAt,
    string? ActorName, string? ActorEmail, string? TenantCode, string? TenantName,
    string? TargetUserName, string? TargetUserEmail, string? IpAddress);

public sealed record PlatformAuditDetail(
    Guid Id, string Action, string Entity, string Description, DateTime CreatedAt,
    string? ActorName, string? ActorEmail, string? TenantCode, string? TenantName,
    string? TargetUserName, string? TargetUserEmail, string? IpAddress, string? UserAgent);
