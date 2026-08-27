namespace SweetSecrets.Application.Common.Recipes;

public sealed record UpdateRecipeItemCommand(
    Guid RecipeId,
    Guid ItemId,
    decimal Quantity);