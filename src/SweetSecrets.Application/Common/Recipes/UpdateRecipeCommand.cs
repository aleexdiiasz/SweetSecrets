namespace SweetSecrets.Application.Common.Recipes;

public sealed record UpdateRecipeCommand(
    Guid RecipeId,
    string Name,
    string? Description,
    decimal Multiplier);