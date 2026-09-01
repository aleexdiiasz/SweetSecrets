using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SweetSecrets.Application.Common.Security;
using SweetSecrets.Application.Common.Users;
using SweetSecrets.Contracts.Admin.Users;

namespace SweetSecrets.Api.Controllers.Admin;

[ApiController]
[Route("api/admin/users")]
[Authorize(Roles = PlatformRoles.PlatformAdmin)]
public sealed class UsersController(IPlatformUserQueryService queryService, IPlatformUserAdminService adminService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PlatformUserPageResponse>> GetUsers(
        string? search, string? role, bool? isBlocked, bool? isOnline,
        int page = 1, int pageSize = 20, CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrWhiteSpace(role) && !PlatformRoles.All.Contains(role, StringComparer.Ordinal))
            return BadRequest(new { message = "El rol seleccionado no es válido." });
        page = Math.Max(1, page); pageSize = Math.Clamp(pageSize, 1, 50);
        var result = await queryService.SearchAsync(
            new PlatformUserSearch(search, role, isBlocked, isOnline, page, pageSize),
            TimeSpan.FromMinutes(5), cancellationToken);
        return Ok(new PlatformUserPageResponse
        {
            Items = result.Items.Select(MapUser).ToList(), TotalCount = result.TotalCount,
            Page = result.Page, PageSize = result.PageSize
        });
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<PlatformUserDetailResponse>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var detail = await queryService.GetAsync(id, TimeSpan.FromMinutes(5), cancellationToken);
        if (detail is null) return NotFound();
        var response = new PlatformUserDetailResponse();
        CopyUser(detail.User, response);
        response.RecentSessions = detail.RecentSessions.Select(MapSession).ToList();
        response.RecentAudit = detail.RecentAudit.Select(x => new PlatformUserAuditItemResponse
            { Action = x.Action, Description = x.Description, CreatedAt = x.CreatedAt }).ToList();
        return Ok(response);
    }

    [HttpPost("{userId:guid}/block")]
    public Task<IActionResult> BlockUser(Guid userId, CancellationToken cancellationToken) =>
        ChangeBlockState(userId, true, cancellationToken);

    [HttpPost("{userId:guid}/unblock")]
    public Task<IActionResult> UnblockUser(Guid userId, CancellationToken cancellationToken) =>
        ChangeBlockState(userId, false, cancellationToken);

    [HttpGet("sessions")]
    public async Task<ActionResult<PlatformSessionPageResponse>> GetSessions(
        string? search, bool? isActive, int page = 1, int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page); pageSize = Math.Clamp(pageSize, 1, 50);
        var result = await queryService.SearchSessionsAsync(
            new PlatformUserSessionSearch(search, isActive, page, pageSize), cancellationToken);
        return Ok(new PlatformSessionPageResponse
        { Items = result.Items.Select(MapSession).ToList(), TotalCount = result.TotalCount, Page = result.Page, PageSize = result.PageSize });
    }

    [HttpPost("sessions/{sessionId:guid}/revoke")]
    public async Task<IActionResult> RevokeSession(Guid sessionId, CancellationToken cancellationToken)
    {
        if (!TryGetActor(out var actorId, out var actorSessionId)) return Unauthorized();
        var outcome = await adminService.RevokeSessionAsync(sessionId, actorId, actorSessionId,
            HttpContext.Connection.RemoteIpAddress?.ToString(), Request.Headers.UserAgent.ToString(), cancellationToken);
        return outcome switch
        {
            PlatformSessionRevokeOutcome.Success => NoContent(),
            PlatformSessionRevokeOutcome.NotFound => NotFound(),
            PlatformSessionRevokeOutcome.AlreadyEnded => Conflict(new { message = "La sesión ya se encuentra cerrada." }),
            _ => Conflict(new { message = "No se permite revocar esta sesión administrativa." })
        };
    }

    private async Task<IActionResult> ChangeBlockState(Guid userId, bool block, CancellationToken cancellationToken)
    {
        if (!TryGetActor(out var actorId, out _)) return Unauthorized();
        var outcome = block
            ? await adminService.BlockUserAsync(userId, actorId, ClientIp(), ClientAgent(), cancellationToken)
            : await adminService.UnblockUserAsync(userId, actorId, ClientIp(), ClientAgent(), cancellationToken);
        return outcome switch
        {
            PlatformUserChangeOutcome.Success => NoContent(),
            PlatformUserChangeOutcome.NotFound => NotFound(),
            PlatformUserChangeOutcome.AlreadyInState => Conflict(new { message = "El usuario ya se encuentra en ese estado." }),
            _ => Conflict(new { message = "Las cuentas PLATFORM_ADMIN son de solo lectura y no pueden bloquearse aquí." })
        };
    }

    private bool TryGetActor(out Guid userId, out Guid? sessionId)
    {
        sessionId = null;
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out userId)) return false;
        if (Guid.TryParse(User.FindFirstValue("session_id"), out var parsed)) sessionId = parsed;
        return true;
    }
    private string? ClientIp() => HttpContext.Connection.RemoteIpAddress?.ToString();
    private string ClientAgent() => Request.Headers.UserAgent.ToString();
    private static PlatformUserListItemResponse MapUser(PlatformUserSummary x) => new()
    { Id = x.Id, Email = x.Email, FullName = x.FullName, Role = x.Role, TenantCode = x.TenantCode,
      TenantName = x.TenantName, IsActive = x.IsActive, IsBlocked = x.IsBlocked,
      EmailConfirmed = x.EmailConfirmed, IsOnline = x.IsOnline, LastLoginAt = x.LastLoginAt,
      LastActivityAt = x.LastActivityAt, CreatedAt = x.CreatedAt };
    private static void CopyUser(PlatformUserSummary x, PlatformUserListItemResponse response)
    { var mapped = MapUser(x); response.Id=mapped.Id; response.Email=mapped.Email; response.FullName=mapped.FullName;
      response.Role=mapped.Role; response.TenantCode=mapped.TenantCode; response.TenantName=mapped.TenantName;
      response.IsActive=mapped.IsActive; response.IsBlocked=mapped.IsBlocked; response.EmailConfirmed=mapped.EmailConfirmed;
      response.IsOnline=mapped.IsOnline; response.LastLoginAt=mapped.LastLoginAt; response.LastActivityAt=mapped.LastActivityAt; response.CreatedAt=mapped.CreatedAt; }
    private static PlatformSessionItemResponse MapSession(PlatformUserSessionSummary x) => new()
    { Id=x.Id, UserId=x.UserId, UserName=x.UserName, Email=x.Email, Role=x.Role, TenantCode=x.TenantCode,
      TenantName=x.TenantName, StartedAt=x.StartedAt, LastActivityAt=x.LastActivityAt, EndedAt=x.EndedAt,
      IsActive=x.IsActive, EndReason=x.EndReason, IpAddress=x.IpAddress, UserAgent=x.UserAgent };
}
