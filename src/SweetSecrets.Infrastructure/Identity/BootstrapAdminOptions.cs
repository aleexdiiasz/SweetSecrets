namespace SweetSecrets.Infrastructure.Identity;

public class BootstrapAdminOptions
{
    public const string SectionName = "BootstrapAdmin";

    public string Email { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;
}