namespace SweetSecrets.Application.Common.Recipes;

public sealed record CreateRecipeCommand(
    string Name,
    string? Description,
    decimal Multiplier);