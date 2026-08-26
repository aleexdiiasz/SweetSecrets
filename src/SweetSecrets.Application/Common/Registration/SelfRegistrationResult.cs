namespace SweetSecrets.Application.Common.Registration;

public sealed record SelfRegistrationResult(
    Guid UserId,
    Guid TenantId,
    string TenantCode,
    string BusinessName,
    string Email);