namespace SweetSecrets.Application.Common.Recipes;

public interface IRecipeQueryService
{
    Task<IReadOnlyList<RecipeListItem>> GetAllAsync(
        CancellationToken cancellationToken = default);

    Task<RecipeDetail?> GetByIdAsync(
        Guid recipeId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<RecipeCostHistoryItem>> GetCostHistoryAsync(
    Guid recipeId,
    CancellationToken cancellationToken = default);
}