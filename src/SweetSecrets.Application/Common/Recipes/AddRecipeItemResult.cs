namespace SweetSecrets.Application.Common.Recipes;

public sealed record AddRecipeItemResult(
    Guid Id,
    Guid RecipeId,
    Guid ProductId,
    decimal Quantity,
    Guid UnitId,
    decimal UnitCost,
    decimal TotalCost);