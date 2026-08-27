namespace SweetSecrets.Application.Common.Recipes;

public sealed record RemoveRecipeItemCommand(
    Guid RecipeId,
    Guid ItemId);