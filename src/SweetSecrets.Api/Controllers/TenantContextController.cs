using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SweetSecrets.Application.Common.Tenancy;
using SweetSecrets.Contracts.Tenancy;

namespace SweetSecrets.Api.Controllers;

[ApiController]
[Route("api/tenant")]
[Authorize]
public class TenantContextController : ControllerBase
{
    private readonly ICurrentTenantResolver _tenantResolver;

    private readonly ICurrentTenantDataService _tenantDataService;

    public TenantContextController(ICurrentTenantResolver tenantResolver, ICurrentTenantDataService tenantDataService)
    {
        _tenantResolver = tenantResolver;
        _tenantDataService = tenantDataService;
    }

    [HttpGet("current")]
    public async Task<ActionResult<CurrentTenantResponse>> GetCurrent(CancellationToken cancellationToken)
    {
        try
        {
            var tenant =
                await _tenantResolver.GetRequiredAsync(
                    cancellationToken);

            return Ok(
                new CurrentTenantResponse
                {
                    TenantId = tenant.TenantId,
                    Code = tenant.Code,
                    Name = tenant.Name
                });
        }
        catch (InvalidOperationException)
        {
            return Forbid();
        }
    }

    [HttpGet("summary")]
    public async Task<ActionResult<CurrentTenantDataSummaryResponse>> GetSummary(    CancellationToken cancellationToken)
    {
        var summary =
            await _tenantDataService.GetSummaryAsync(
                cancellationToken);

        return Ok(
            new CurrentTenantDataSummaryResponse
            {
                Units = summary.Units,
                Products = summary.Products,
                Recipes = summary.Recipes
            });
    }
}