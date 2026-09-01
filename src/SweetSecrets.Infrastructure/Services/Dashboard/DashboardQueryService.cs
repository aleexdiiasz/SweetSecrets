using Microsoft.EntityFrameworkCore;
using SweetSecrets.Application.Common.Dashboard;
using SweetSecrets.Infrastructure.Data.Tenant;

namespace SweetSecrets.Infrastructure.Services.Dashboard;

public sealed class DashboardQueryService : IDashboardQueryService
{
    private const int RecentItemsLimit = 5;
    private readonly ITenantDbContextFactory _dbContextFactory;

    public DashboardQueryService(ITenantDbContextFactory dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    public async Task<DashboardSummary> GetAsync(
        CancellationToken cancellationToken = default)
    {
        await using var dbContext =
            await _dbContextFactory.CreateAsync(cancellationToken);

        var productMetrics = await dbContext.Products
            .AsNoTracking()
            .GroupBy(_ => 1)
            .Select(group => new
            {
                Total = group.Count(),
                Active = group.Count(product => product.IsActive)
            })
            .FirstOrDefaultAsync(cancellationToken);

        var recipeMetrics = await dbContext.Recipes
            .AsNoTracking()
            .GroupBy(_ => 1)
            .Select(group => new
            {
                Total = group.Count(),
                Active = group.Count(recipe => recipe.IsActive),
                AverageActiveCost = group
                    .Where(recipe => recipe.IsActive)
                    .Select(recipe => (decimal?)recipe.TotalCost)
                    .Average() ?? 0m
            })
            .FirstOrDefaultAsync(cancellationToken);

        var recentRecipes = await dbContext.Recipes
            .AsNoTracking()
            .OrderByDescending(recipe => recipe.UpdatedAt ?? recipe.CreatedAt)
            .ThenBy(recipe => recipe.Name)
            .Take(RecentItemsLimit)
            .Select(recipe => new RecentRecipe(
                recipe.Id,
                recipe.Name,
                recipe.TotalCost,
                recipe.SuggestedPrice,
                recipe.IsActive,
                recipe.UpdatedAt ?? recipe.CreatedAt))
            .ToListAsync(cancellationToken);

        var recentProducts = await dbContext.Products
            .AsNoTracking()
            .OrderByDescending(product => product.UpdatedAt ?? product.CreatedAt)
            .ThenBy(product => product.Name)
            .Take(RecentItemsLimit)
            .Select(product => new RecentProduct(
                product.Id,
                product.Name,
                product.PurchasePrice,
                product.UnitCost,
                product.IsActive,
                product.UpdatedAt ?? product.CreatedAt))
            .ToListAsync(cancellationToken);

        return new DashboardSummary(
            productMetrics?.Total ?? 0,
            productMetrics?.Active ?? 0,
            recipeMetrics?.Total ?? 0,
            recipeMetrics?.Active ?? 0,
            recipeMetrics?.AverageActiveCost ?? 0m,
            recentRecipes,
            recentProducts);
    }
}
