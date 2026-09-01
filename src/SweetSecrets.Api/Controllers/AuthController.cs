using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SweetSecrets.Contracts.Auth;
using SweetSecrets.Application.Common.Authentication;
using SweetSecrets.Application.Common.Registration;
using SweetSecrets.Application.Common.Security;

namespace SweetSecrets.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly SweetSecrets.Application.Common.Authentication.IAuthenticationService _authenticationService;
    private readonly IAccountService _accountService;
    private readonly IEmailConfirmationService _emailConfirmationService;
    private readonly SweetSecrets.Application.Common.Authentication.IPasswordRecoveryService _passwordRecoveryService;
    private readonly ISelfRegistrationService _selfRegistrationService;

    public AuthController(
        SweetSecrets.Application.Common.Authentication.IAuthenticationService authenticationService,
        IAccountService accountService,
        IEmailConfirmationService emailConfirmationService,
        SweetSecrets.Application.Common.Authentication.IPasswordRecoveryService passwordRecoveryService,
        ISelfRegistrationService selfRegistrationService)
    {
        _authenticationService = authenticationService;
        _accountService = accountService;
        _emailConfirmationService = emailConfirmationService;
        _passwordRecoveryService = passwordRecoveryService;
        _selfRegistrationService = selfRegistrationService;
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login(LoginRequest request, CancellationToken cancellationToken)
    {
        var result = await _authenticationService.LoginAsync(
            request.Email,
            request.Password,
            request.RememberMe,
            HttpContext.Connection.RemoteIpAddress?.ToString(),
            Request.Headers.UserAgent.ToString(),
            cancellationToken);

        if (!result.Succeeded)
        {
            return Unauthorized(new
            {
                result.ErrorCode,
                result.Message
            });
        }

        return Ok(new LoginResponse
        {
            UserId = result.UserId!.Value,
            TenantId = result.TenantId,
            Roles = result.Roles,
            Message = "Inicio de sesión correcto."
        });
    }

    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        var userIdValue =
            User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!Guid.TryParse(userIdValue, out var userId))
            return Unauthorized();

        Guid? sessionId = null;

        var sessionValue =
            User.FindFirstValue("session_id");

        if (Guid.TryParse(sessionValue, out var parsedSessionId))
            sessionId = parsedSessionId;

        await _authenticationService.LogoutAsync(
            userId,
            sessionId,
            HttpContext.Connection.RemoteIpAddress?.ToString(),
            Request.Headers.UserAgent.ToString(),
            cancellationToken);

        return NoContent();
    }

    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<ActionResult<RegisterResponse>> Register(RegisterRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var result =
                await _selfRegistrationService.RegisterAsync(
                    new SelfRegistrationCommand(
                        request.BusinessName,
                        request.FullName,
                        request.Email,
                        request.Password),
                    cancellationToken);

            return Ok(
                new RegisterResponse
                {
                    UserId = result.UserId,
                    TenantId = result.TenantId,
                    TenantCode = result.TenantCode,
                    BusinessName = result.BusinessName,
                    Email = result.Email
                });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(
                new
                {
                    message = ex.Message
                });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(
                new
                {
                    message = ex.Message
                });
        }
    }

    [HttpPost("forgot-password")]
    [AllowAnonymous]
    public async Task<ActionResult<ForgotPasswordResponse>> ForgotPassword(
        ForgotPasswordRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            await _passwordRecoveryService.RequestResetAsync(request.Email, cancellationToken);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }

        return Ok(new ForgotPasswordResponse
        {
            Message = "Si existe una cuenta asociada a este correo, recibirás instrucciones para restablecer tu contraseña."
        });
    }

    [HttpPost("reset-password")]
    [AllowAnonymous]
    public async Task<ActionResult<ResetPasswordResponse>> ResetPassword(
        ResetPasswordRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _passwordRecoveryService.ResetPasswordAsync(
            request.Email,
            request.Token,
            request.NewPassword,
            cancellationToken);

        if (!result.Succeeded)
            return BadRequest(new { message = result.ErrorMessage });

        return Ok(new ResetPasswordResponse
        {
            Message = "Tu contraseña se actualizó correctamente."
        });
    }

    [HttpPost("resend-confirmation")]
    [AllowAnonymous]
    public async Task<ActionResult<ResendEmailConfirmationResponse>> ResendConfirmation(
        ResendEmailConfirmationRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            await _emailConfirmationService.RequestResendAsync(request.Email, cancellationToken);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }

        return Ok(new ResendEmailConfirmationResponse
        {
            Message = "Si existe una cuenta pendiente asociada a este correo, recibirás instrucciones para confirmarla."
        });
    }

    [HttpPost("confirm-email")]
    [AllowAnonymous]
    public async Task<ActionResult<ConfirmEmailResponse>> ConfirmEmail(
        ConfirmEmailRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _emailConfirmationService.ConfirmAsync(
            request.Email,
            request.Token,
            cancellationToken);

        if (!result.Succeeded)
            return BadRequest(new { message = result.ErrorMessage });

        return Ok(new ConfirmEmailResponse
        {
            Message = "Tu correo electrónico se confirmó correctamente. Ya puedes iniciar sesión."
        });
    }

    [Authorize]
    [HttpGet("me")]
    public ActionResult<CurrentUserResponse> Me()
    {
        var userIdValue =
            User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!Guid.TryParse(userIdValue, out var userId))
            return Unauthorized();

        Guid? tenantId = null;

        var tenantValue =
            User.FindFirstValue("tenant_id");

        if (Guid.TryParse(tenantValue, out var parsedTenantId))
            tenantId = parsedTenantId;

        Guid? sessionId = null;

        var sessionValue =
            User.FindFirstValue("session_id");

        if (Guid.TryParse(sessionValue, out var parsedSessionId))
            sessionId = parsedSessionId;

        var roles = User.FindAll(ClaimTypes.Role)
            .Select(x => x.Value)
            .Distinct()
            .ToList();

        return Ok(new CurrentUserResponse
        {
            UserId = userId,
            TenantId = tenantId,
            SessionId = sessionId,
            Email =
                User.FindFirstValue(ClaimTypes.Email)
                ?? User.Identity?.Name
                ?? string.Empty,
            Roles = roles
        });
    }

    [Authorize(Roles = PlatformRoles.TenantOwner)]
    [HttpGet("account")]
    public async Task<ActionResult<AccountResponse>> Account(CancellationToken cancellationToken)
    {
        if (!TryGetAuthenticatedUserId(out var userId))
            return Unauthorized();

        var account = await _accountService.GetAsync(userId, cancellationToken);

        if (account is null)
            return Unauthorized();

        return Ok(new AccountResponse
        {
            FullName = account.FullName,
            Email = account.Email
        });
    }

    [Authorize(Roles = PlatformRoles.TenantOwner)]
    [HttpPost("change-password")]
    public async Task<ActionResult<ChangePasswordResponse>> ChangePassword(
        ChangePasswordRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetAuthenticatedUserId(out var userId))
            return Unauthorized();

        var result = await _accountService.ChangePasswordAsync(
            userId,
            request.CurrentPassword,
            request.NewPassword,
            cancellationToken);

        if (!result.Succeeded)
            return BadRequest(new { message = result.ErrorMessage });

        return Ok(new ChangePasswordResponse
        {
            Message = "Tu contraseña se actualizó correctamente. La sesión actual continúa activa."
        });
    }

    private bool TryGetAuthenticatedUserId(out Guid userId)
    {
        return Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out userId);
    }
}
