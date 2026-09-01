using Microsoft.AspNetCore.Identity;
using SweetSecrets.Application.Common.Auditing;
using SweetSecrets.Application.Common.Sessions;
using SweetSecrets.Application.Common.Users;
using SweetSecrets.Infrastructure.Identity;
using SweetSecrets.Application.Common.Security;
using SweetSecrets.Infrastructure.Data.Master;
using Microsoft.EntityFrameworkCore;

namespace SweetSecrets.Infrastructure.Services.Users;

public class PlatformUserAdminService : IPlatformUserAdminService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IUserSessionService _userSessionService;
    private readonly IPlatformAuditService _auditService;
    private readonly MasterDbContext _dbContext;

    public PlatformUserAdminService(
        UserManager<ApplicationUser> userManager,
        IUserSessionService userSessionService,
        IPlatformAuditService auditService,
        MasterDbContext dbContext)
    {
        _userManager = userManager;
        _userSessionService = userSessionService;
        _auditService = auditService;
        _dbContext = dbContext;
    }

    public async Task<PlatformUserChangeOutcome> BlockUserAsync(
        Guid userId,
        Guid performedByUserId,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());

        if (user is null)
            return PlatformUserChangeOutcome.NotFound;

        if (user.Id == performedByUserId)
        {
            return PlatformUserChangeOutcome.Forbidden;
        }

        if (await _userManager.IsInRoleAsync(user, PlatformRoles.PlatformAdmin))
            return PlatformUserChangeOutcome.Forbidden;

        if (user.IsBlocked)
            return PlatformUserChangeOutcome.AlreadyInState;

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

        return PlatformUserChangeOutcome.Success;
    }

    public async Task<PlatformUserChangeOutcome> UnblockUserAsync(
        Guid userId,
        Guid performedByUserId,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());

        if (user is null)
            return PlatformUserChangeOutcome.NotFound;

        if (await _userManager.IsInRoleAsync(user, PlatformRoles.PlatformAdmin))
            return PlatformUserChangeOutcome.Forbidden;

        if (!user.IsBlocked)
            return PlatformUserChangeOutcome.AlreadyInState;

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

        return PlatformUserChangeOutcome.Success;
    }

    public async Task<PlatformSessionRevokeOutcome> RevokeSessionAsync(
        Guid sessionId,
        Guid performedByUserId,
        Guid? performedFromSessionId,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken = default)
    {
        var session = await _dbContext.UserSessions.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == sessionId, cancellationToken);
        if (session is null) return PlatformSessionRevokeOutcome.NotFound;
        if (!session.IsActive) return PlatformSessionRevokeOutcome.AlreadyEnded;
        if (session.Id == performedFromSessionId) return PlatformSessionRevokeOutcome.Forbidden;

        var target = await _userManager.FindByIdAsync(session.UserId.ToString());
        if (target is null) return PlatformSessionRevokeOutcome.NotFound;
        if (await _userManager.IsInRoleAsync(target, PlatformRoles.PlatformAdmin))
            return PlatformSessionRevokeOutcome.Forbidden;

        await _userSessionService.EndSessionAsync(sessionId, "SESSION_REVOKED", cancellationToken);
        await _auditService.RegisterAsync(new PlatformAuditEntry
        {
            UserId = performedByUserId,
            TenantId = target.TenantId,
            Action = "SESSION_REVOKED",
            Entity = "USER",
            EntityId = target.Id.ToString(),
            Description = $"Se revocó una sesión de {target.Email}.",
            IpAddress = ipAddress,
            UserAgent = userAgent
        }, cancellationToken);
        return PlatformSessionRevokeOutcome.Success;
    }
}
