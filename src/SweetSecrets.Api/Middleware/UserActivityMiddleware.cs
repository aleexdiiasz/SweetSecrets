using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using SweetSecrets.Application.Common.Sessions;
using SweetSecrets.Infrastructure.Identity;

namespace SweetSecrets.Api.Middleware;

public class UserActivityMiddleware
{
    private readonly RequestDelegate _next;

    public UserActivityMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(
        HttpContext context,
        IUserSessionService sessionService,
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager)
    {
        if (context.User.Identity?.IsAuthenticated == true)
        {
            await UpdateActivityAsync(
                context,
                sessionService,
                userManager,
                signInManager);
        }

        await _next(context);
    }

    private static async Task UpdateActivityAsync(
        HttpContext context,
        IUserSessionService sessionService,
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager)
    {
        var sessionValue = context.User.FindFirstValue("session_id");

        var userValue = context.User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!Guid.TryParse(sessionValue, out var sessionId) || !Guid.TryParse(userValue, out var userId))
        {
            return;
        }

        if (!await sessionService.ValidateAndUpdateActivityAsync(sessionId, userId, context.RequestAborted))
        {
            await signInManager.SignOutAsync();
            context.User = new ClaimsPrincipal(new ClaimsIdentity());
            return;
        }

        var user = await userManager.FindByIdAsync(userId.ToString());

        if (user is null)
            return;

        user.LastActivityAt = DateTime.UtcNow;

        await userManager.UpdateAsync(user);
    }
}
