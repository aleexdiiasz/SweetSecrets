namespace SweetSecrets.Application.Common.Recipes;

public sealed record RecipeDetail(
    Guid Id,
    string Name,
    string? Description,
    decimal Multiplier,
    decimal TotalCost,
    decimal SuggestedPrice,
    bool IsActive,
    DateTime CreatedAt,
    Guid? CreatedBy,
    DateTime? UpdatedAt,
    Guid? UpdatedBy,
    IReadOnlyList<RecipeItemDetail> Items);