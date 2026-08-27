namespace SweetSecrets.Application.Common.Recipes;

public sealed record CreateRecipeResult(
    Guid Id,
    string Name,
    string? Description,
    decimal Multiplier,
    decimal TotalCost,
    decimal SuggestedPrice);