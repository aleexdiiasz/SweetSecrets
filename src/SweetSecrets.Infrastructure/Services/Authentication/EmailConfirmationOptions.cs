namespace SweetSecrets.Infrastructure.Services.Authentication;

public sealed class EmailConfirmationOptions
{
    public const string SectionName = "EmailConfirmation";

    public string ConfirmationPageBaseUrl { get; set; } = string.Empty;

    public DateTimeOffset EnforceForAccountsCreatedAfterUtc { get; set; } =
        new(2026, 9, 1, 0, 0, 0, TimeSpan.Zero);
}
