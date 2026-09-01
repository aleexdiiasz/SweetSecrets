namespace SweetSecrets.Infrastructure.Services.Authentication;

public sealed class PasswordRecoveryOptions
{
    public const string SectionName = "PasswordRecovery";

    public string ResetPageBaseUrl { get; set; } = string.Empty;
}
