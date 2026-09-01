using SweetSecrets.Domain.Enums;

namespace SweetSecrets.Application.Common.Tenants;

public sealed record PlatformTenantSearch(
    string? Search,
    TenantStatus? Status,
    int Page,
    int PageSize);

public sealed record PlatformTenantPage(
    IReadOnlyList<PlatformTenantSummary> Items,
    int TotalCount,
    int Page,
    int PageSize);

public sealed record PlatformTenantSummary(
    Guid Id,
    string Code,
    string Name,
    TenantStatus Status,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    string? OwnerName,
    string? OwnerEmail);

public sealed record PlatformTenantDetail(
    Guid Id,
    string Code,
    string Name,
    TenantStatus Status,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    Guid? OwnerId,
    string? OwnerName,
    string? OwnerEmail,
    DateTime? OwnerLastLoginAt,
    DateTime? OwnerLastActivityAt,
    IReadOnlyList<PlatformTenantAuditItem> RecentAudit);

public sealed record PlatformTenantAuditItem(
    string Action,
    string Description,
    DateTime CreatedAt);

public enum TenantStatusChangeOutcome
{
    Success,
    NotFound,
    InvalidTransition,
    StateChanged
}
