using Microsoft.EntityFrameworkCore;
using SweetSecrets.Application.Common.Recipes;
using SweetSecrets.Application.Common.Tenancy;
using SweetSecrets.Infrastructure.Data.Tenant;

namespace SweetSecrets.Infrastructure.Services.Recipes;

public sealed class RecipeQueryService : IRecipeQueryService
{
    private readonly ITenantDbContextFactory _dbContextFactory;

    public RecipeQueryService(ITenantDbContextFactory dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    public async Task<IReadOnlyList<RecipeListItem>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        await using var dbContext =
            await _dbContextFactory.CreateAsync(cancellationToken);

        return await dbContext.Recipes
            .AsNoTracking()
            .OrderBy(x => x.Name)
            .Select(x => new RecipeListItem(
                x.Id,
                x.Name,
                x.Description,
                x.Multiplier,
                x.TotalCost,
                x.SuggestedPrice,
                x.IsActive))
            .ToListAsync(cancellationToken);
    }

    public async Task<RecipeDetail?> GetByIdAsync(Guid recipeId, CancellationToken cancellationToken = default)
    {
        await using var dbContext =
            await _dbContextFactory.CreateAsync(cancellationToken);

        return await dbContext.Recipes
            .AsNoTracking()
            .Where(x => x.Id == recipeId)
            .Select(x => new RecipeDetail(
                x.Id,
                x.Name,
                x.Description,
                x.Multiplier,
                x.TotalCost,
                x.SuggestedPrice,
                x.IsActive,
                x.CreatedAt,
                x.CreatedBy,
                x.UpdatedAt,
                x.UpdatedBy,
                x.Items
                    .OrderBy(item => item.Product.Name)
                    .Select(item => new RecipeItemDetail(
                        item.Id,
                        item.ProductId,
                        item.Product.Name,
                        item.Quantity,
                        item.UnitId,
                        item.Unit.Code,
                        item.Unit.Name,
                        item.Unit.Symbol,
                        item.UnitCost,
                        item.TotalCost))
                    .ToList()))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<RecipeCostHistoryItem>> GetCostHistoryAsync(Guid recipeId, CancellationToken cancellationToken = default)
    {
        await using var dbContext =
            await _dbContextFactory.CreateAsync(cancellationToken);

        return await dbContext.RecipeCostHistory
            .AsNoTracking()
            .Where(x => x.RecipeId == recipeId)
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new RecipeCostHistoryItem(
                x.Id,
                x.RecipeId,
                x.PreviousCost,
                x.NewCost,
                x.Reason,
                x.CreatedAt))
            .ToListAsync(cancellationToken);
    }
}