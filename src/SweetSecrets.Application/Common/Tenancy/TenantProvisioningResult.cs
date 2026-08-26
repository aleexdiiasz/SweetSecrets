namespace SweetSecrets.Application.Common.Tenancy;

public sealed record TenantProvisioningResult(
    Guid TenantId,
    string Code,
    string Name,
    string DatabaseName);