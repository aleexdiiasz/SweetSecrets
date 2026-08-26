namespace SweetSecrets.Application.Common.Tenancy;

public sealed record CurrentTenantInfo(
    Guid TenantId,
    string Code,
    string Name,
    string DatabaseName);