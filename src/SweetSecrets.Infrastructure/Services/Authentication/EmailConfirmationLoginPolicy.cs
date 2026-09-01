using Microsoft.Extensions.Options;
using SweetSecrets.Infrastructure.Identity;

namespace SweetSecrets.Infrastructure.Services.Authentication;

public sealed class EmailConfirmationLoginPolicy
{
    private readonly EmailConfirmationOptions _options;

    public EmailConfirmationLoginPolicy(IOptions<EmailConfirmationOptions> options)
    {
        _options = options.Value;
    }

    public bool RequiresConfirmation(ApplicationUser user) =>
        !user.EmailConfirmed &&
        user.CreatedAt >= _options.EnforceForAccountsCreatedAfterUtc.UtcDateTime;
}
