namespace SweetSecrets.Application.Common.Recipes;

public sealed record RecipeListItem(
    Guid Id,
    string Name,
    string? Description,
    decimal Multiplier,
    decimal TotalCost,
    decimal SuggestedPrice,
    bool IsActive);