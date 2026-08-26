using Microsoft.AspNetCore.Identity;
using SweetSecrets.Application.Common.Auditing;
using SweetSecrets.Application.Common.Sessions;
using SweetSecrets.Application.Common.Users;
using SweetSecrets.Infrastructure.Identity;

namespace SweetSecrets.Infrastructure.Services.Users;

public class PlatformUserAdminService : IPlatformUserAdminService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IUserSessionService _userSessionService;
    private readonly IPlatformAuditService _auditService;

    public PlatformUserAdminService(
        UserManager<ApplicationUser> userManager,
        IUserSessionService userSessionService,
        IPlatformAuditService auditService)
    {
        _userManager = userManager;
        _userSessionService = userSessionService;
        _auditService = auditService;
    }

    public async Task BlockUserAsync(
        Guid userId,
        Guid performedByUserId,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());

        if (user is null)
            throw new InvalidOperationException("Usuario no encontrado.");

        if (user.Id == performedByUserId)
        {
            throw new InvalidOperationException(
                "El administrador no puede bloquear su propia cuenta.");
        }

        if (user.IsBlocked)
            return;

        user.IsBlocked = true;

        var updateResult = await _userManager.UpdateAsync(user);

        if (!updateResult.Succeeded)
        {
            throw new InvalidOperationException(
                "No fue posible bloquear el usuario: " +
                string.Join(", ",
                    updateResult.Errors.Select(x => x.Description)));
        }

        await _userManager.UpdateSecurityStampAsync(user);

        await _userSessionService.EndAllUserSessionsAsync(
            user.Id,
            "USER_BLOCKED",
            cancellationToken);

        await _auditService.RegisterAsync(
            new PlatformAuditEntry
            {
                UserId = performedByUserId,
                TenantId = user.TenantId,

                Action = "USER_BLOCKED",
                Entity = "USER",
                EntityId = user.Id.ToString(),

                Description =
                    $"Se bloqueó al usuario {user.Email}.",

                NewValues = """
                            {
                              "IsBlocked": true
                            }
                            """,

                IpAddress = ipAddress,
                UserAgent = userAgent
            },
            cancellationToken);
    }

    public async Task UnblockUserAsync(
        Guid userId,
        Guid performedByUserId,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());

        if (user is null)
            throw new InvalidOperationException("Usuario no encontrado.");

        if (!user.IsBlocked)
            return;

        user.IsBlocked = false;

        var updateResult = await _userManager.UpdateAsync(user);

        if (!updateResult.Succeeded)
        {
            throw new InvalidOperationException(
                "No fue posible desbloquear el usuario: " +
                string.Join(", ",
                    updateResult.Errors.Select(x => x.Description)));
        }

        await _userManager.UpdateSecurityStampAsync(user);

        await _auditService.RegisterAsync(
            new PlatformAuditEntry
            {
                UserId = performedByUserId,
                TenantId = user.TenantId,

                Action = "USER_UNBLOCKED",
                Entity = "USER",
                EntityId = user.Id.ToString(),

                Description =
                    $"Se desbloqueó al usuario {user.Email}.",

                OldValues = """
                            {
                              "IsBlocked": true
                            }
                            """,

                NewValues = """
                            {
                              "IsBlocked": false
                            }
                            """,

                IpAddress = ipAddress,
                UserAgent = userAgent
            },
            cancellationToken);
    }
}