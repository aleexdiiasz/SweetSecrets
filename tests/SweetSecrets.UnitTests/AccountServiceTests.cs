using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using SweetSecrets.Application.Common.Authentication;
using SweetSecrets.Infrastructure.Identity;
using SweetSecrets.Infrastructure.Services.Authentication;

namespace SweetSecrets.UnitTests;

public sealed class AccountServiceTests
{
    private const string CurrentPassword = "Actual123!";

    [Fact]
    public async Task ChangePasswordAsync_RejectsIncorrectCurrentPassword()
    {
        var fixture = await CreateFixtureAsync();
        Assert.IsType<SpanishIdentityErrorDescriber>(fixture.UserManager.ErrorDescriber);

        var result = await fixture.Service.ChangePasswordAsync(
            fixture.User.Id,
            "Incorrecta123!",
            "NuevaClave123!");

        Assert.False(result.Succeeded);
        Assert.Equal("La contraseña actual es incorrecta.", result.ErrorMessage);
        Assert.Equal(0, fixture.SessionRefresher.RefreshCount);
    }

    [Fact]
    public async Task ChangePasswordAsync_ReturnsLocalizedErrorsForInvalidNewPassword()
    {
        var fixture = await CreateFixtureAsync();
        Assert.IsType<SpanishIdentityErrorDescriber>(fixture.UserManager.ErrorDescriber);

        var result = await fixture.Service.ChangePasswordAsync(
            fixture.User.Id,
            CurrentPassword,
            "invalida");

        Assert.False(result.Succeeded);
        Assert.Contains("La contraseña debe tener al menos 10 caracteres.", result.ErrorMessage);
        Assert.Contains("La contraseña debe contener al menos un carácter especial.", result.ErrorMessage);
        Assert.Contains("La contraseña debe contener al menos un dígito.", result.ErrorMessage);
        Assert.Contains("La contraseña debe contener al menos una letra mayúscula.", result.ErrorMessage);
        Assert.Equal(0, fixture.SessionRefresher.RefreshCount);
    }

    [Fact]
    public async Task ChangePasswordAsync_ChangesPasswordAndRefreshesCurrentSession()
    {
        var fixture = await CreateFixtureAsync();

        var result = await fixture.Service.ChangePasswordAsync(
            fixture.User.Id,
            CurrentPassword,
            "NuevaClave123!");

        Assert.True(result.Succeeded);
        Assert.True(await fixture.UserManager.CheckPasswordAsync(fixture.User, "NuevaClave123!"));
        Assert.Equal(1, fixture.SessionRefresher.RefreshCount);
        Assert.Equal(fixture.User.Id, fixture.SessionRefresher.RefreshedUserId);
    }

    private static async Task<AccountServiceFixture> CreateFixtureAsync()
    {
        var options = Options.Create(new IdentityOptions());
        options.Value.Password.RequiredLength = 10;
        options.Value.Password.RequireDigit = true;
        options.Value.Password.RequireLowercase = true;
        options.Value.Password.RequireUppercase = true;
        options.Value.Password.RequireNonAlphanumeric = true;

        var store = new InMemoryUserPasswordStore();
        var describer = new SpanishIdentityErrorDescriber();
        var manager = new UserManager<ApplicationUser>(
            store,
            options,
            new PasswordHasher<ApplicationUser>(),
            [],
            [new PasswordValidator<ApplicationUser>()],
            new UpperInvariantLookupNormalizer(),
            describer,
            null!,
            NullLogger<UserManager<ApplicationUser>>.Instance);
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = "owner@example.com",
            Email = "owner@example.com",
            FullName = "Owner",
            IsActive = true
        };
        var createResult = await manager.CreateAsync(user, CurrentPassword);
        Assert.True(createResult.Succeeded);

        var refresher = new RecordingAccountSessionRefresher();
        var localizer = new IdentityErrorLocalizer(options);
        return new AccountServiceFixture(user, manager, refresher, new AccountService(manager, refresher, localizer));
    }

    private sealed record AccountServiceFixture(
        ApplicationUser User,
        UserManager<ApplicationUser> UserManager,
        RecordingAccountSessionRefresher SessionRefresher,
        AccountService Service);

    private sealed class RecordingAccountSessionRefresher : IAccountSessionRefresher
    {
        public int RefreshCount { get; private set; }
        public Guid? RefreshedUserId { get; private set; }

        public Task RefreshAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            RefreshCount++;
            RefreshedUserId = userId;
            return Task.CompletedTask;
        }
    }

    private sealed class InMemoryUserPasswordStore : IUserPasswordStore<ApplicationUser>
    {
        private readonly Dictionary<Guid, ApplicationUser> _users = [];

        public Task<IdentityResult> CreateAsync(ApplicationUser user, CancellationToken cancellationToken)
        {
            _users[user.Id] = user;
            return Task.FromResult(IdentityResult.Success);
        }

        public Task<ApplicationUser?> FindByIdAsync(string userId, CancellationToken cancellationToken) =>
            Task.FromResult(Guid.TryParse(userId, out var id) && _users.TryGetValue(id, out var user) ? user : null);

        public Task<ApplicationUser?> FindByNameAsync(string normalizedUserName, CancellationToken cancellationToken) =>
            Task.FromResult(_users.Values.FirstOrDefault(user => user.NormalizedUserName == normalizedUserName));

        public Task<string?> GetNormalizedUserNameAsync(ApplicationUser user, CancellationToken cancellationToken) => Task.FromResult(user.NormalizedUserName);
        public Task<string?> GetPasswordHashAsync(ApplicationUser user, CancellationToken cancellationToken) => Task.FromResult(user.PasswordHash);
        public Task<string> GetUserIdAsync(ApplicationUser user, CancellationToken cancellationToken) => Task.FromResult(user.Id.ToString());
        public Task<string?> GetUserNameAsync(ApplicationUser user, CancellationToken cancellationToken) => Task.FromResult(user.UserName);
        public Task<bool> HasPasswordAsync(ApplicationUser user, CancellationToken cancellationToken) => Task.FromResult(user.PasswordHash is not null);
        public Task SetNormalizedUserNameAsync(ApplicationUser user, string? normalizedName, CancellationToken cancellationToken) { user.NormalizedUserName = normalizedName; return Task.CompletedTask; }
        public Task SetPasswordHashAsync(ApplicationUser user, string? passwordHash, CancellationToken cancellationToken) { user.PasswordHash = passwordHash; return Task.CompletedTask; }
        public Task SetUserNameAsync(ApplicationUser user, string? userName, CancellationToken cancellationToken) { user.UserName = userName; return Task.CompletedTask; }
        public Task<IdentityResult> UpdateAsync(ApplicationUser user, CancellationToken cancellationToken) { _users[user.Id] = user; return Task.FromResult(IdentityResult.Success); }
        public Task<IdentityResult> DeleteAsync(ApplicationUser user, CancellationToken cancellationToken) { _users.Remove(user.Id); return Task.FromResult(IdentityResult.Success); }
        public void Dispose() { }
    }
}
