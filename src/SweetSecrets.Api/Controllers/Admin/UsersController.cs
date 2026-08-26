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
public class UsersController : ControllerBase
{
    private readonly IPlatformUserQueryService _queryService;
    private readonly IPlatformUserAdminService _adminService;

    public UsersController(IPlatformUserQueryService queryService, IPlatformUserAdminService adminService)
    {
        _queryService = queryService;
        _adminService = adminService;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<PlatformUserResponse>>> GetUsers(
        CancellationToken cancellationToken)
    {
        var users = await _queryService.GetUsersAsync(TimeSpan.FromMinutes(5), cancellationToken);

        var response = users
            .Select(x => new PlatformUserResponse
            {
                Id = x.Id,
                TenantId = x.TenantId,
                Email = x.Email,
                FullName = x.FullName,
                IsActive = x.IsActive,
                IsBlocked = x.IsBlocked,
                IsOnline = x.IsOnline,
                LastLoginAt = x.LastLoginAt,
                LastActivityAt = x.LastActivityAt,
                CreatedAt = x.CreatedAt
            })
            .ToList();

        return Ok(response);
    }

    [HttpPost("{userId:guid}/block")]
    public async Task<IActionResult> BlockUser(Guid userId, CancellationToken cancellationToken)
    {
        var adminUserId = GetCurrentUserId();

        await _adminService.BlockUserAsync(
            userId,
            adminUserId,
            HttpContext.Connection.RemoteIpAddress?.ToString(),
            Request.Headers.UserAgent.ToString(),
            cancellationToken);

        return NoContent();
    }

    [HttpPost("{userId:guid}/unblock")]
    public async Task<IActionResult> UnblockUser(Guid userId, CancellationToken cancellationToken)
    {
        var adminUserId = GetCurrentUserId();

        await _adminService.UnblockUserAsync(
            userId,
            adminUserId,
            HttpContext.Connection.RemoteIpAddress?.ToString(),
            Request.Headers.UserAgent.ToString(),
            cancellationToken);

        return NoContent();
    }

    private Guid GetCurrentUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!Guid.TryParse(value, out var userId))
        {
            throw new UnauthorizedAccessException(
                "No fue posible identificar al usuario autenticado.");
        }

        return userId;
    }
}