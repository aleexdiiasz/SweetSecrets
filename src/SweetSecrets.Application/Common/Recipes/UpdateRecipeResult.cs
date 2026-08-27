namespace SweetSecrets.Application.Common.Recipes;

public sealed record UpdateRecipeResult(
    Guid Id,
    string Name,
    string? Description,
    decimal Multiplier,
    decimal TotalCost,
    decimal SuggestedPrice,
    DateTime UpdatedAt,
    Guid UpdatedBy);