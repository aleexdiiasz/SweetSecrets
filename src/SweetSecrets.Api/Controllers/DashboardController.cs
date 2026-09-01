using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SweetSecrets.Application.Common.Dashboard;
using SweetSecrets.Application.Common.Security;
using SweetSecrets.Contracts.Dashboard;

namespace SweetSecrets.Api.Controllers;

[ApiController]
[Route("api/dashboard")]
[Authorize(Roles = PlatformRoles.TenantOwner)]
public sealed class DashboardController : ControllerBase
{
    private readonly IDashboardQueryService _dashboardQueryService;

    public DashboardController(IDashboardQueryService dashboardQueryService)
    {
        _dashboardQueryService = dashboardQueryService;
    }

    [HttpGet]
    public async Task<ActionResult<DashboardResponse>> Get(
        CancellationToken cancellationToken)
    {
        var dashboard =
            await _dashboardQueryService.GetAsync(cancellationToken);

        return Ok(new DashboardResponse
        {
            TotalProducts = dashboard.TotalProducts,
            ActiveProducts = dashboard.ActiveProducts,
            TotalRecipes = dashboard.TotalRecipes,
            ActiveRecipes = dashboard.ActiveRecipes,
            AverageActiveRecipeCost = dashboard.AverageActiveRecipeCost,
            RecentRecipes = dashboard.RecentRecipes.Select(recipe => new RecentRecipeResponse
            {
                Id = recipe.Id,
                Name = recipe.Name,
                TotalCost = recipe.TotalCost,
                SuggestedPrice = recipe.SuggestedPrice,
                IsActive = recipe.IsActive,
                ActivityAt = recipe.ActivityAt
            }).ToList(),
            RecentProducts = dashboard.RecentProducts.Select(product => new RecentProductResponse
            {
                Id = product.Id,
                Name = product.Name,
                PurchasePrice = product.PurchasePrice,
                UnitCost = product.UnitCost,
                IsActive = product.IsActive,
                ActivityAt = product.ActivityAt
            }).ToList()
        });
    }
}
