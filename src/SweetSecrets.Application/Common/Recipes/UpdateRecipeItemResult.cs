namespace SweetSecrets.Application.Common.Recipes;

public sealed record UpdateRecipeItemResult(
    Guid Id,
    Guid RecipeId,
    Guid ProductId,
    decimal Quantity,
    Guid UnitId,
    decimal UnitCost,
    decimal TotalCost,
    decimal RecipeTotalCost,
    decimal RecipeSuggestedPrice);