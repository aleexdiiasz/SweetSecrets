using Microsoft.AspNetCore.Identity;
using SweetSecrets.Application.Common.Authentication;
using SweetSecrets.Infrastructure.Identity;

namespace SweetSecrets.Infrastructure.Services.Authentication;

public sealed class AccountService : IAccountService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IAccountSessionRefresher _sessionRefresher;
    private readonly IdentityErrorLocalizer _errorLocalizer;

    public AccountService(
        UserManager<ApplicationUser> userManager,
        IAccountSessionRefresher sessionRefresher,
        IdentityErrorLocalizer errorLocalizer)
    {
        _userManager = userManager;
        _sessionRefresher = sessionRefresher;
        _errorLocalizer = errorLocalizer;
    }

    public async Task<AccountInfo?> GetAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());

        return user is null || !user.IsActive || user.IsBlocked
            ? null
            : new AccountInfo(user.FullName, user.Email ?? string.Empty);
    }

    public async Task<ChangePasswordResult> ChangePasswordAsync(
        Guid userId,
        string currentPassword,
        string newPassword,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(currentPassword))
            return ChangePasswordResult.Failed("La contraseña actual es obligatoria.");

        if (string.IsNullOrWhiteSpace(newPassword))
            return ChangePasswordResult.Failed("La nueva contraseña es obligatoria.");

        var user = await _userManager.FindByIdAsync(userId.ToString());

        if (user is null || !user.IsActive || user.IsBlocked)
            return ChangePasswordResult.Failed("No fue posible cambiar la contraseña.");

        var result = await _userManager.ChangePasswordAsync(user, currentPassword, newPassword);

        if (!result.Succeeded)
            return ChangePasswordResult.Failed(_errorLocalizer.Localize(result.Errors));

        await _sessionRefresher.RefreshAsync(user.Id, cancellationToken);

        return ChangePasswordResult.Success();
    }
}
