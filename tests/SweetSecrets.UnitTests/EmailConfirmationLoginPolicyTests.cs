using Microsoft.Extensions.Options;
using SweetSecrets.Infrastructure.Identity;
using SweetSecrets.Infrastructure.Services.Authentication;

namespace SweetSecrets.UnitTests;

public sealed class EmailConfirmationLoginPolicyTests
{
    private static readonly DateTimeOffset Cutoff = new(2026, 9, 1, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public void NewUnconfirmedAccount_RequiresConfirmationBeforeLogin()
    {
        var policy = CreatePolicy();
        var user = new ApplicationUser { CreatedAt = Cutoff.UtcDateTime.AddMinutes(1), EmailConfirmed = false };

        Assert.True(policy.RequiresConfirmation(user));
    }

    [Fact]
    public void ConfirmedAccount_CanLogin()
    {
        var policy = CreatePolicy();
        var user = new ApplicationUser { CreatedAt = Cutoff.UtcDateTime.AddMinutes(1), EmailConfirmed = true };

        Assert.False(policy.RequiresConfirmation(user));
    }

    [Fact]
    public void LegacyUnconfirmedAccount_IsNotBlockedRetroactively()
    {
        var policy = CreatePolicy();
        var user = new ApplicationUser { CreatedAt = Cutoff.UtcDateTime.AddMinutes(-1), EmailConfirmed = false };

        Assert.False(policy.RequiresConfirmation(user));
    }

    private static EmailConfirmationLoginPolicy CreatePolicy() => new(
        Options.Create(new EmailConfirmationOptions { EnforceForAccountsCreatedAfterUtc = Cutoff }));
}
