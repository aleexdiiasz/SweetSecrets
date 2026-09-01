using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SweetSecrets.Application.Common.Dashboard;
using SweetSecrets.Application.Common.Security;
using SweetSecrets.Contracts.Admin.Dashboard;

namespace SweetSecrets.Api.Controllers.Admin;

[ApiController]
[Route("api/admin/dashboard")]
[Authorize(Roles = PlatformRoles.PlatformAdmin)]
public sealed class PlatformDashboardController(IPlatformDashboardQueryService queryService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PlatformDashboardResponse>> Get(CancellationToken cancellationToken)
    {
        var summary = await queryService.GetAsync(TimeSpan.FromMinutes(5), 5, cancellationToken);
        return Ok(new PlatformDashboardResponse
        {
            TotalTenants = summary.TotalTenants,
            TenantsByStatus = summary.TenantsByStatus.ToDictionary(x => x.Key.ToString(), x => x.Value),
            TotalUsers = summary.TotalUsers,
            TenantOwners = summary.TenantOwners,
            PlatformAdmins = summary.PlatformAdmins,
            BlockedUsers = summary.BlockedUsers,
            PendingEmailConfirmation = summary.PendingEmailConfirmation,
            ActiveSessions = summary.ActiveSessions,
            OnlineUsers = summary.OnlineUsers,
            RecentTenants = summary.RecentTenants.Select(x => new PlatformRecentTenantResponse
                { Id=x.Id, Code=x.Code, Name=x.Name, Status=x.Status.ToString(), CreatedAt=x.CreatedAt }).ToList(),
            RecentUsers = summary.RecentUsers.Select(x => new PlatformRecentUserResponse
                { Id=x.Id, FullName=x.FullName, Email=x.Email, Role=x.Role, CreatedAt=x.CreatedAt }).ToList(),
            RecentEvents = summary.RecentEvents.Select(x => new PlatformRecentEventResponse
                { Action=x.Action, Description=x.Description, CreatedAt=x.CreatedAt }).ToList()
        });
    }
}
