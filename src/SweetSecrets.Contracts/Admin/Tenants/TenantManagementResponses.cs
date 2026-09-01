namespace SweetSecrets.Contracts.Admin.Tenants;

public sealed class TenantPageResponse
{
    public IReadOnlyList<TenantListItemResponse> Items { get; set; } = [];
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}

public class TenantListItemResponse
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? OwnerName { get; set; }
    public string? OwnerEmail { get; set; }
}

public sealed class TenantDetailResponse : TenantListItemResponse
{
    public Guid? OwnerId { get; set; }
    public DateTime? OwnerLastLoginAt { get; set; }
    public DateTime? OwnerLastActivityAt { get; set; }
    public IReadOnlyList<TenantAuditItemResponse> RecentAudit { get; set; } = [];
}

public sealed class TenantAuditItemResponse
{
    public string Action { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
