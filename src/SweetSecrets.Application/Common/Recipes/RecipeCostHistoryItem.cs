namespace SweetSecrets.Application.Common.Recipes;

public sealed record RecipeCostHistoryItem(
    Guid Id,
    Guid RecipeId,
    decimal PreviousCost,
    decimal NewCost,
    string Reason,
    DateTime CreatedAt);