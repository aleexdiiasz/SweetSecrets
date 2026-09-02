namespace SweetSecrets.Contracts.Admin.Audit;

public sealed class PlatformAuditPageResponse
{
    public IReadOnlyList<PlatformAuditItemResponse> Items { get; set; } = [];
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}

public class PlatformAuditItemResponse
{
    public Guid Id { get; set; }
    public string Action { get; set; } = string.Empty;
    public string Entity { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string? ActorName { get; set; }
    public string? ActorEmail { get; set; }
    public string? TenantCode { get; set; }
    public string? TenantName { get; set; }
    public string? TargetUserName { get; set; }
    public string? TargetUserEmail { get; set; }
    public string? IpAddress { get; set; }
}

public sealed class PlatformAuditDetailResponse : PlatformAuditItemResponse
{
    public string? UserAgent { get; set; }
}
