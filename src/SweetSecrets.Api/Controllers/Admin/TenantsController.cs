using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SweetSecrets.Application.Common.Security;
using SweetSecrets.Application.Common.Tenancy;
using SweetSecrets.Contracts.Admin.Tenants;
using SweetSecrets.Application.Common.Tenants;
using SweetSecrets.Domain.Enums;
using System.Security.Claims;

namespace SweetSecrets.Api.Controllers.Admin;

[ApiController]
[Route("api/admin/tenants")]
[Authorize(Roles = PlatformRoles.PlatformAdmin)]
public class TenantsController : ControllerBase
{
    private readonly ITenantProvisioningService _provisioningService;
    private readonly ITenantUserProvisioningService _tenantUserProvisioningService;
    private readonly IPlatformTenantAdminService _tenantAdminService;

    public TenantsController(ITenantProvisioningService provisioningService, ITenantUserProvisioningService tenantUserProvisioningService, IPlatformTenantAdminService tenantAdminService)
    {
        _provisioningService = provisioningService;

        _tenantUserProvisioningService = tenantUserProvisioningService;
        _tenantAdminService = tenantAdminService;
    }

    [HttpGet]
    public async Task<ActionResult<TenantPageResponse>> GetAll(string? search, string? status, int page = 1, int pageSize = 20, CancellationToken cancellationToken = default)
    {
        TenantStatus? parsedStatus = null;
        if (!string.IsNullOrWhiteSpace(status))
        {
            if (!Enum.TryParse<TenantStatus>(status, true, out var value))
                return BadRequest(new { message = "El estado seleccionado no es válido." });
            parsedStatus = value;
        }

        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 50);
        var result = await _tenantAdminService.SearchAsync(new PlatformTenantSearch(search, parsedStatus, page, pageSize), cancellationToken);
        return Ok(new TenantPageResponse
        {
            Items = result.Items.Select(MapListItem).ToList(),
            TotalCount = result.TotalCount,
            Page = result.Page,
            PageSize = result.PageSize
        });
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<TenantDetailResponse>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var tenant = await _tenantAdminService.GetAsync(id, cancellationToken);
        if (tenant is null)
            return NotFound();

        return Ok(new TenantDetailResponse
        {
            Id = tenant.Id, Code = tenant.Code, Name = tenant.Name, Status = tenant.Status.ToString(),
            CreatedAt = tenant.CreatedAt, UpdatedAt = tenant.UpdatedAt, OwnerId = tenant.OwnerId,
            OwnerName = tenant.OwnerName, OwnerEmail = tenant.OwnerEmail,
            OwnerLastLoginAt = tenant.OwnerLastLoginAt, OwnerLastActivityAt = tenant.OwnerLastActivityAt,
            RecentAudit = tenant.RecentAudit.Select(item => new TenantAuditItemResponse
            { Action = item.Action, Description = item.Description, CreatedAt = item.CreatedAt }).ToList()
        });
    }

    [HttpPost("{id:guid}/suspend")]
    public Task<IActionResult> Suspend(Guid id, CancellationToken cancellationToken) =>
        ChangeStatus(id, TenantStatus.Suspended, cancellationToken);

    [HttpPost("{id:guid}/activate")]
    public Task<IActionResult> Activate(Guid id, CancellationToken cancellationToken) =>
        ChangeStatus(id, TenantStatus.Active, cancellationToken);

    private async Task<IActionResult> ChangeStatus(Guid tenantId, TenantStatus target, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var actorUserId))
            return Unauthorized();

        var outcome = await _tenantAdminService.ChangeStatusAsync(tenantId, target, actorUserId,
            HttpContext.Connection.RemoteIpAddress?.ToString(), Request.Headers.UserAgent.ToString(), cancellationToken);

        return outcome switch
        {
            TenantStatusChangeOutcome.Success => NoContent(),
            TenantStatusChangeOutcome.NotFound => NotFound(),
            TenantStatusChangeOutcome.InvalidTransition => Conflict(new { message = "El tenant no permite esta transición en su estado actual." }),
            _ => Conflict(new { message = "El estado del tenant cambió. Actualiza la información e inténtalo nuevamente." })
        };
    }

    private static TenantListItemResponse MapListItem(PlatformTenantSummary tenant) => new()
    {
        Id = tenant.Id, Code = tenant.Code, Name = tenant.Name, Status = tenant.Status.ToString(),
        CreatedAt = tenant.CreatedAt, UpdatedAt = tenant.UpdatedAt,
        OwnerName = tenant.OwnerName, OwnerEmail = tenant.OwnerEmail
    };

    [HttpPost("provision")]
    public async Task<ActionResult<ProvisionTenantResponse>> Provision(ProvisionTenantRequest request, CancellationToken cancellationToken)
    {
        var result =
            await _provisioningService.ProvisionAsync(
                request.Name,
                cancellationToken);

        return Ok(
            new ProvisionTenantResponse
            {
                TenantId = result.TenantId,
                Code = result.Code,
                Name = result.Name,
                DatabaseName = result.DatabaseName
            });
    }

    [HttpPost("owner")]
    public async Task<ActionResult<CreateTenantOwnerResponse>> CreateOwner(CreateTenantOwnerRequest request, CancellationToken cancellationToken)
    {
        var userId =
            await _tenantUserProvisioningService.CreateOwnerAsync(
                request.TenantId,
                request.Email,
                request.FullName,
                request.Password,
                cancellationToken);

        return Ok(
            new CreateTenantOwnerResponse
            {
                UserId = userId,
                TenantId = request.TenantId,
                Email = request.Email.Trim()
            });
    }
}
