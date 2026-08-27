namespace SweetSecrets.Application.Common.Recipes;

public sealed record AddRecipeItemCommand(
    Guid RecipeId,
    Guid ProductId,
    decimal Quantity,
    Guid UnitId);