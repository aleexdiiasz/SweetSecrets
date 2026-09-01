using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SweetSecrets.Application.Common.Auditing;
using SweetSecrets.Application.Common.Security;
using SweetSecrets.Contracts.Admin.Audit;

namespace SweetSecrets.Api.Controllers.Admin;

[ApiController]
[Route("api/admin/audit")]
[Authorize(Roles = PlatformRoles.PlatformAdmin)]
public sealed class AuditController(IPlatformAuditQueryService queryService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PlatformAuditPageResponse>> GetAll(
        string? search, string? action, DateTime? from, DateTime? to,
        string? tenant, string? actor, string? targetUser,
        int page = 1, int pageSize = 20, CancellationToken cancellationToken = default)
    {
        if (from.HasValue && to.HasValue && from.Value.Date > to.Value.Date)
            return BadRequest(new { message = "La fecha inicial no puede ser posterior a la fecha final." });
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 50);
        var result = await queryService.SearchAsync(new PlatformAuditSearch(search, action?.Trim().ToUpperInvariant(),
            AsUtcDate(from), AsUtcDate(to)?.AddDays(1), tenant, actor, targetUser, page, pageSize), cancellationToken);
        return Ok(new PlatformAuditPageResponse
        {
            Items = result.Items.Select(Map).ToList(), TotalCount = result.TotalCount,
            Page = result.Page, PageSize = result.PageSize
        });
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<PlatformAuditDetailResponse>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var item = await queryService.GetAsync(id, cancellationToken);
        if (item is null) return NotFound();
        var response = new PlatformAuditDetailResponse
        {
            Id=item.Id, Action=item.Action, Entity=item.Entity, Description=item.Description,
            CreatedAt=item.CreatedAt, ActorName=item.ActorName, ActorEmail=item.ActorEmail,
            TenantCode=item.TenantCode, TenantName=item.TenantName,
            TargetUserName=item.TargetUserName, TargetUserEmail=item.TargetUserEmail,
            IpAddress=item.IpAddress, UserAgent=item.UserAgent
        };
        return Ok(response);
    }

    private static PlatformAuditItemResponse Map(PlatformAuditSummary item)
    {
        var response = new PlatformAuditItemResponse(); Copy(item, response); return response;
    }

    private static void Copy(PlatformAuditSummary item, PlatformAuditItemResponse response)
    {
        response.Id=item.Id; response.Action=item.Action; response.Entity=item.Entity; response.Description=item.Description;
        response.CreatedAt=item.CreatedAt; response.ActorName=item.ActorName; response.ActorEmail=item.ActorEmail;
        response.TenantCode=item.TenantCode; response.TenantName=item.TenantName;
        response.TargetUserName=item.TargetUserName; response.TargetUserEmail=item.TargetUserEmail; response.IpAddress=item.IpAddress;
    }

    private static DateTime? AsUtcDate(DateTime? value) => value.HasValue
        ? DateTime.SpecifyKind(value.Value.Date, DateTimeKind.Utc)
        : null;
}
