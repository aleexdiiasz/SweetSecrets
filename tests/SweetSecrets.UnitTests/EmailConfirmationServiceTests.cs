using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using SweetSecrets.Application.Common.Email;
using SweetSecrets.Infrastructure.Identity;
using SweetSecrets.Infrastructure.Services.Authentication;

namespace SweetSecrets.UnitTests;

public sealed class EmailConfirmationServiceTests
{
    [Fact]
    public async Task SendForUserAsync_GeneratesIdentityTokenAndConfirmationLink()
    {
        var fixture = CreateFixture();
        var generatedToken = await fixture.UserManager.GenerateEmailConfirmationTokenAsync(fixture.User);
        Assert.NotEmpty(generatedToken);

        await fixture.Service.SendForUserAsync(fixture.User.Id);

        var message = Assert.Single(fixture.EmailSender.Messages);
        Assert.Equal(fixture.User.Email, message.Recipient);
        Assert.Contains("/confirm-email?", message.TextBody, StringComparison.Ordinal);
        Assert.Contains("token=", message.TextBody, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ConfirmAsync_WithGeneratedToken_ConfirmsEmail()
    {
        var fixture = CreateFixture();
        await fixture.Service.SendForUserAsync(fixture.User.Id);
        var link = new Uri(fixture.EmailSender.Messages.Single().TextBody.Split(Environment.NewLine).Last());
        var query = QueryHelpers.ParseQuery(link.Query);

        var result = await fixture.Service.ConfirmAsync(
            query["email"].ToString(),
            query["token"].ToString());

        Assert.True(result.Succeeded);
        Assert.True(fixture.User.EmailConfirmed);
    }

    [Fact]
    public async Task ConfirmAsync_WithInvalidToken_DoesNotConfirmEmail()
    {
        var fixture = CreateFixture();

        var result = await fixture.Service.ConfirmAsync(fixture.User.Email!, "invalid-token");

        Assert.False(result.Succeeded);
        Assert.False(fixture.User.EmailConfirmed);
        Assert.Equal("El enlace de confirmación no es válido o ya expiró.", result.ErrorMessage);
    }

    [Fact]
    public async Task RequestResendAsync_DoesNotRevealMissingOrConfirmedAccounts()
    {
        var fixture = CreateFixture();

        await fixture.Service.RequestResendAsync("missing@example.com");
        Assert.Empty(fixture.EmailSender.Messages);

        fixture.User.EmailConfirmed = true;
        await fixture.Service.RequestResendAsync(fixture.User.Email!);
        Assert.Empty(fixture.EmailSender.Messages);

        fixture.User.EmailConfirmed = false;
        await fixture.Service.RequestResendAsync(fixture.User.Email!);
        Assert.Single(fixture.EmailSender.Messages);
    }

    private static Fixture CreateFixture()
    {
        var store = new InMemoryEmailUserStore();
        var options = Options.Create(new IdentityOptions());
        var userManager = new UserManager<ApplicationUser>(
            store,
            options,
            new PasswordHasher<ApplicationUser>(),
            [],
            [],
            new UpperInvariantLookupNormalizer(),
            new SpanishIdentityErrorDescriber(),
            null!,
            NullLogger<UserManager<ApplicationUser>>.Instance);
        userManager.RegisterTokenProvider(
            options.Value.Tokens.EmailConfirmationTokenProvider,
            new DeterministicEmailTokenProvider());
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = "owner@example.com",
            NormalizedUserName = "OWNER@EXAMPLE.COM",
            Email = "owner@example.com",
            NormalizedEmail = "OWNER@EXAMPLE.COM",
            FullName = "Owner",
            IsActive = true,
            EmailConfirmed = false,
            CreatedAt = DateTime.UtcNow
        };
        store.Seed(user);
        var sender = new RecordingEmailSender();
        var service = new EmailConfirmationService(
            userManager,
            sender,
            Options.Create(new EmailConfirmationOptions
            {
                ConfirmationPageBaseUrl = "https://localhost:7011/confirm-email"
            }),
            NullLogger<EmailConfirmationService>.Instance);

        return new Fixture(user, userManager, sender, service);
    }

    private sealed record Fixture(
        ApplicationUser User,
        UserManager<ApplicationUser> UserManager,
        RecordingEmailSender EmailSender,
        EmailConfirmationService Service);

    private sealed class RecordingEmailSender : ITransactionalEmailSender
    {
        public List<TransactionalEmailMessage> Messages { get; } = [];

        public Task SendAsync(TransactionalEmailMessage message, CancellationToken cancellationToken = default)
        {
            Messages.Add(message);
            return Task.CompletedTask;
        }
    }

    private sealed class DeterministicEmailTokenProvider : IUserTwoFactorTokenProvider<ApplicationUser>
    {
        public Task<string> GenerateAsync(string purpose, UserManager<ApplicationUser> manager, ApplicationUser user) =>
            Task.FromResult($"token:{purpose}:{user.Id}");

        public Task<bool> ValidateAsync(string purpose, string token, UserManager<ApplicationUser> manager, ApplicationUser user) =>
            Task.FromResult(token == $"token:{purpose}:{user.Id}");

        public Task<bool> CanGenerateTwoFactorTokenAsync(UserManager<ApplicationUser> manager, ApplicationUser user) =>
            Task.FromResult(false);
    }

    private sealed class InMemoryEmailUserStore : IUserEmailStore<ApplicationUser>
    {
        private readonly Dictionary<Guid, ApplicationUser> _users = [];

        public void Seed(ApplicationUser user) => _users[user.Id] = user;
        public Task<ApplicationUser?> FindByIdAsync(string userId, CancellationToken cancellationToken) => Task.FromResult(Guid.TryParse(userId, out var id) && _users.TryGetValue(id, out var user) ? user : null);
        public Task<ApplicationUser?> FindByNameAsync(string normalizedUserName, CancellationToken cancellationToken) => Task.FromResult(_users.Values.FirstOrDefault(user => user.NormalizedUserName == normalizedUserName));
        public Task<ApplicationUser?> FindByEmailAsync(string normalizedEmail, CancellationToken cancellationToken) => Task.FromResult(_users.Values.FirstOrDefault(user => user.NormalizedEmail == normalizedEmail));
        public Task<string?> GetEmailAsync(ApplicationUser user, CancellationToken cancellationToken) => Task.FromResult(user.Email);
        public Task<bool> GetEmailConfirmedAsync(ApplicationUser user, CancellationToken cancellationToken) => Task.FromResult(user.EmailConfirmed);
        public Task<string?> GetNormalizedEmailAsync(ApplicationUser user, CancellationToken cancellationToken) => Task.FromResult(user.NormalizedEmail);
        public Task<string?> GetNormalizedUserNameAsync(ApplicationUser user, CancellationToken cancellationToken) => Task.FromResult(user.NormalizedUserName);
        public Task<string> GetUserIdAsync(ApplicationUser user, CancellationToken cancellationToken) => Task.FromResult(user.Id.ToString());
        public Task<string?> GetUserNameAsync(ApplicationUser user, CancellationToken cancellationToken) => Task.FromResult(user.UserName);
        public Task SetEmailAsync(ApplicationUser user, string? email, CancellationToken cancellationToken) { user.Email = email; return Task.CompletedTask; }
        public Task SetEmailConfirmedAsync(ApplicationUser user, bool confirmed, CancellationToken cancellationToken) { user.EmailConfirmed = confirmed; return Task.CompletedTask; }
        public Task SetNormalizedEmailAsync(ApplicationUser user, string? normalizedEmail, CancellationToken cancellationToken) { user.NormalizedEmail = normalizedEmail; return Task.CompletedTask; }
        public Task SetNormalizedUserNameAsync(ApplicationUser user, string? normalizedName, CancellationToken cancellationToken) { user.NormalizedUserName = normalizedName; return Task.CompletedTask; }
        public Task SetUserNameAsync(ApplicationUser user, string? userName, CancellationToken cancellationToken) { user.UserName = userName; return Task.CompletedTask; }
        public Task<IdentityResult> CreateAsync(ApplicationUser user, CancellationToken cancellationToken) { _users[user.Id] = user; return Task.FromResult(IdentityResult.Success); }
        public Task<IdentityResult> UpdateAsync(ApplicationUser user, CancellationToken cancellationToken) { _users[user.Id] = user; return Task.FromResult(IdentityResult.Success); }
        public Task<IdentityResult> DeleteAsync(ApplicationUser user, CancellationToken cancellationToken) { _users.Remove(user.Id); return Task.FromResult(IdentityResult.Success); }
        public void Dispose() { }
    }
}
