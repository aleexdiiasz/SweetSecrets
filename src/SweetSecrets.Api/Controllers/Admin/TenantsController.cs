using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SweetSecrets.Application.Common.Security;
using SweetSecrets.Application.Common.Tenancy;
using SweetSecrets.Contracts.Admin.Tenants;

namespace SweetSecrets.Api.Controllers.Admin;

[ApiController]
[Route("api/admin/tenants")]
[Authorize(Roles = PlatformRoles.PlatformAdmin)]
public class TenantsController : ControllerBase
{
    private readonly ITenantProvisioningService _provisioningService;
    private readonly ITenantUserProvisioningService _tenantUserProvisioningService;

    public TenantsController(ITenantProvisioningService provisioningService, ITenantUserProvisioningService tenantUserProvisioningService)
    {
        _provisioningService = provisioningService;

        _tenantUserProvisioningService = tenantUserProvisioningService;
    }

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