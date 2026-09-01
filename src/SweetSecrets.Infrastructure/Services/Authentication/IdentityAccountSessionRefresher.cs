using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using SweetSecrets.Application.Common.Authentication;
using SweetSecrets.Infrastructure.Identity;

namespace SweetSecrets.Infrastructure.Services.Authentication;

public sealed class IdentityAccountSessionRefresher : IAccountSessionRefresher
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;

    public IdentityAccountSessionRefresher(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager)
    {
        _userManager = userManager;
        _signInManager = signInManager;
    }

    public async Task RefreshAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString())
            ?? throw new InvalidOperationException("No fue posible actualizar la sesión de la cuenta.");
        var context = _signInManager.Context;
        var authentication = await context.AuthenticateAsync(IdentityConstants.ApplicationScheme);
        var additionalClaims = context.User.Claims
            .Where(claim => claim.Type is "session_id" or "tenant_id")
            .Select(claim => new Claim(claim.Type, claim.Value))
            .ToList();

        await _signInManager.SignInWithClaimsAsync(
            user,
            authentication.Properties ?? new AuthenticationProperties(),
            additionalClaims);
    }
}
