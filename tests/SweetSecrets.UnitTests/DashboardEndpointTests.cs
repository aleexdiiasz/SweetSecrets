using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SweetSecrets.Api.Controllers;
using SweetSecrets.Application.Common.Dashboard;
using SweetSecrets.Application.Common.Security;
using SweetSecrets.Contracts.Dashboard;

namespace SweetSecrets.UnitTests;

public sealed class DashboardEndpointTests
{
    [Fact]
    public void Controller_RequiresTenantOwner()
    {
        var authorize = Assert.Single(typeof(DashboardController).GetCustomAttributes(typeof(AuthorizeAttribute), true).Cast<AuthorizeAttribute>());
        Assert.Equal(PlatformRoles.TenantOwner, authorize.Roles);
    }

    [Fact]
    public async Task Get_MapsTenantSummaryAndRecentActivity()
    {
        var at = new DateTime(2026, 9, 1, 12, 0, 0, DateTimeKind.Utc);
        var service = new StubDashboardQueryService(new DashboardSummary(
            12, 10, 4, 3, 25.50m,
            [new RecentRecipe(Guid.NewGuid(), "Pastel", 30m, 120m, true, at)],
            [new RecentProduct(Guid.NewGuid(), "Harina", 42m, 0.042m, true, at)]));

        var result = await new DashboardController(service).Get(default);
        var response = Assert.IsType<DashboardResponse>(Assert.IsType<OkObjectResult>(result.Result).Value);

        Assert.Equal(12, response.TotalProducts);
        Assert.Equal(10, response.ActiveProducts);
        Assert.Equal(4, response.TotalRecipes);
        Assert.Equal(3, response.ActiveRecipes);
        Assert.Equal(25.50m, response.AverageActiveRecipeCost);
        Assert.Single(response.RecentRecipes);
        Assert.Single(response.RecentProducts);
    }

    [Fact]
    public async Task Get_ReturnsZeroesAndEmptyListsForEmptyTenant()
    {
        var service = new StubDashboardQueryService(new DashboardSummary(0, 0, 0, 0, 0m, [], []));
        var result = await new DashboardController(service).Get(default);
        var response = Assert.IsType<DashboardResponse>(Assert.IsType<OkObjectResult>(result.Result).Value);

        Assert.Equal(0, response.TotalProducts);
        Assert.Equal(0, response.TotalRecipes);
        Assert.Empty(response.RecentRecipes);
        Assert.Empty(response.RecentProducts);
    }

    private sealed class StubDashboardQueryService(DashboardSummary summary) : IDashboardQueryService
    {
        public Task<DashboardSummary> GetAsync(CancellationToken cancellationToken = default) => Task.FromResult(summary);
    }
}
