namespace SweetSecrets.Application.Common.Recipes;

public sealed record RemoveRecipeItemResult(
    Guid RecipeId,
    Guid ItemId,
    decimal RecipeTotalCost,
    decimal RecipeSuggestedPrice);