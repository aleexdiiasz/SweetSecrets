namespace SweetSecrets.Application.Common.Registration;

public sealed record SelfRegistrationCommand(
    string BusinessName,
    string FullName,
    string Email,
    string Password);