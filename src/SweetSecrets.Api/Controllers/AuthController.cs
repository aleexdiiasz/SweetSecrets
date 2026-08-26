using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SweetSecrets.Contracts.Auth;
using SweetSecrets.Application.Common.Registration;

namespace SweetSecrets.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly SweetSecrets.Application.Common.Authentication.IAuthenticationService _authenticationService;
    private readonly ISelfRegistrationService _selfRegistrationService;

    public AuthController(SweetSecrets.Application.Common.Authentication.IAuthenticationService authenticationService, ISelfRegistrationService selfRegistrationService)
    {
        _authenticationService = authenticationService;
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
}