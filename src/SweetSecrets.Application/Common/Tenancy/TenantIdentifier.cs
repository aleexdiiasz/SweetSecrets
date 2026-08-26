namespace SweetSecrets.Application.Common.Tenancy;

public sealed record TenantIdentifier(
    long Number,
    string Code,
    string DatabaseName);