using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using SweetSecrets.Application.Common.Auditing;
using SweetSecrets.Application.Common.Authentication;
using SweetSecrets.Application.Common.Sessions;
using SweetSecrets.Infrastructure.Identity;

namespace SweetSecrets.Infrastructure.Services.Authentication;

public class AuthenticationService
    : SweetSecrets.Application.Common.Authentication.IAuthenticationService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IUserSessionService _sessionService;
    private readonly IPlatformAuditService _auditService;
    private readonly EmailConfirmationLoginPolicy _emailConfirmationPolicy;
    private readonly ITenantLoginPolicy _tenantLoginPolicy;

    public AuthenticationService(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        IUserSessionService sessionService,
        IPlatformAuditService auditService,
        EmailConfirmationLoginPolicy emailConfirmationPolicy,
        ITenantLoginPolicy tenantLoginPolicy)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _sessionService = sessionService;
        _auditService = auditService;
        _emailConfirmationPolicy = emailConfirmationPolicy;
        _tenantLoginPolicy = tenantLoginPolicy;
    }

    public async Task<AuthenticationResult> LoginAsync(
        string email,
        string password,
        bool rememberMe,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(email) ||
            string.IsNullOrWhiteSpace(password))
        {
            return InvalidCredentials();
        }

        email = email.Trim();

        var user = await _userManager.FindByEmailAsync(email);

        if (user is null)
        {
            return InvalidCredentials();
        }

        if (!user.IsActive)
        {
            return new AuthenticationResult
            {
                Succeeded = false,
                ErrorCode = "ACCOUNT_INACTIVE",
                Message = "La cuenta se encuentra inactiva."
            };
        }

        if (user.IsBlocked)
        {
            return new AuthenticationResult
            {
                Succeeded = false,
                ErrorCode = "ACCOUNT_BLOCKED",
                Message = "La cuenta se encuentra bloqueada."
            };
        }

        var passwordResult =
            await _signInManager.CheckPasswordSignInAsync(
                user,
                password,
                lockoutOnFailure: true);

        if (passwordResult.IsLockedOut)
        {
            await RegisterFailedLoginAsync(
                user,
                "LOGIN_LOCKED_OUT",
                "La cuenta fue bloqueada temporalmente por intentos fallidos.",
                ipAddress,
                userAgent,
                cancellationToken);

            return new AuthenticationResult
            {
                Succeeded = false,
                ErrorCode = "LOCKED_OUT",
                Message =
                    "La cuenta está bloqueada temporalmente por intentos fallidos."
            };
        }

        if (!passwordResult.Succeeded)
        {
            await RegisterFailedLoginAsync(
                user,
                "LOGIN_FAILED",
                "Intento de inicio de sesión fallido.",
                ipAddress,
                userAgent,
                cancellationToken);

            return InvalidCredentials();
        }

        if (_emailConfirmationPolicy.RequiresConfirmation(user))
        {
            return new AuthenticationResult
            {
                Succeeded = false,
                ErrorCode = "EMAIL_NOT_CONFIRMED",
                Message = "Debes confirmar tu correo electrónico antes de iniciar sesión."
            };
        }

        var roles = (await _userManager.GetRolesAsync(user)).ToList();
        var tenantDecision = await _tenantLoginPolicy.EvaluateAsync(
            user.TenantId,
            roles,
            cancellationToken);

        if (tenantDecision != TenantLoginDecision.Allowed)
            return TenantAccessDenied(tenantDecision);

        var sessionId =
            await _sessionService.StartSessionAsync(
                user.Id,
                ipAddress,
                userAgent,
                cancellationToken);

        var now = DateTime.UtcNow;

        user.LastLoginAt = now;
        user.LastActivityAt = now;

        var updateResult = await _userManager.UpdateAsync(user);

        if (!updateResult.Succeeded)
        {
            await _sessionService.EndSessionAsync(
                sessionId,
                "LOGIN_INITIALIZATION_FAILED",
                cancellationToken);

            throw new InvalidOperationException(
                "No fue posible actualizar el acceso del usuario: " +
                string.Join(
                    ", ",
                    updateResult.Errors.Select(x => x.Description)));
        }

        var claims = new List<Claim>
        {
            new("session_id", sessionId.ToString())
        };

        if (user.TenantId.HasValue)
        {
            claims.Add(
                new Claim(
                    "tenant_id",
                    user.TenantId.Value.ToString()));
        }

        var authenticationProperties =
            new AuthenticationProperties
            {
                IsPersistent = rememberMe,
                AllowRefresh = true
            };

        await _signInManager.SignInWithClaimsAsync(
            user,
            authenticationProperties,
            claims);

        await _auditService.RegisterAsync(
            new PlatformAuditEntry
            {
                UserId = user.Id,
                TenantId = user.TenantId,

                Action = "LOGIN_SUCCESS",
                Entity = "USER",
                EntityId = user.Id.ToString(),

                Description =
                    $"Inicio de sesión exitoso para {user.Email}.",

                IpAddress = ipAddress,
                UserAgent = userAgent
            },
            cancellationToken);

        return new AuthenticationResult
        {
            Succeeded = true,
            UserId = user.Id,
            SessionId = sessionId,
            TenantId = user.TenantId,
            Roles = roles.ToList()
        };
    }

    private async Task RegisterFailedLoginAsync(
        ApplicationUser user,
        string action,
        string description,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken)
    {
        await _auditService.RegisterAsync(
            new PlatformAuditEntry
            {
                UserId = user.Id,
                TenantId = user.TenantId,

                Action = action,
                Entity = "USER",
                EntityId = user.Id.ToString(),

                Description = description,

                IpAddress = ipAddress,
                UserAgent = userAgent
            },
            cancellationToken);
    }

    public async Task LogoutAsync(
    Guid userId,
    Guid? sessionId,
    string? ipAddress,
    string? userAgent,
    CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());

        if (sessionId.HasValue)
        {
            await _sessionService.EndSessionAsync(
                sessionId.Value,
                "LOGOUT",
                cancellationToken);
        }

        await _signInManager.SignOutAsync();

        await _auditService.RegisterAsync(
            new PlatformAuditEntry
            {
                UserId = userId,
                TenantId = user?.TenantId,

                Action = "LOGOUT",
                Entity = "USER",
                EntityId = userId.ToString(),

                Description = "Cierre de sesión exitoso.",

                IpAddress = ipAddress,
                UserAgent = userAgent
            },
            cancellationToken);
    }

    private static AuthenticationResult InvalidCredentials()
    {
        return new AuthenticationResult
        {
            Succeeded = false,
            ErrorCode = "INVALID_CREDENTIALS",
            Message = "Correo o contraseña incorrectos."
        };
    }

    private static AuthenticationResult TenantAccessDenied(
        TenantLoginDecision decision)
    {
        return new AuthenticationResult
        {
            Succeeded = false,
            ErrorCode = decision == TenantLoginDecision.Suspended
                ? "TENANT_SUSPENDED"
                : "TENANT_UNAVAILABLE",
            Message = decision == TenantLoginDecision.Suspended
                ? "La cuenta se encuentra suspendida. Contacta al administrador de la plataforma."
                : "La cuenta no está disponible para iniciar sesión. Contacta al administrador de la plataforma."
        };
    }
}
