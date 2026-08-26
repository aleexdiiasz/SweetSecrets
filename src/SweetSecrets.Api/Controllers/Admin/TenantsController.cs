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

    public TenantsController(        ITenantProvisioningService provisioningService)
    {
        _provisioningService = provisioningService;
    }

    [HttpPost("provision")]
    public async Task<ActionResult<ProvisionTenantResponse>> Provision(        ProvisionTenantRequest request,        CancellationToken cancellationToken)
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
}