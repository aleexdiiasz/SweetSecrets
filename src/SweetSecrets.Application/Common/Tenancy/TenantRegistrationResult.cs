namespace SweetSecrets.Application.Common.Tenancy;

public sealed record TenantRegistrationResult(
    Guid TenantId,
    string Code,
    string Name,
    string DatabaseName);