namespace SweetSecrets.Application.Common.Dashboard;

public sealed record DashboardSummary(
    int TotalProducts,
    int ActiveProducts,
    int TotalRecipes,
    int ActiveRecipes,
    decimal AverageActiveRecipeCost,
    IReadOnlyList<RecentRecipe> RecentRecipes,
    IReadOnlyList<RecentProduct> RecentProducts);

public sealed record RecentRecipe(
    Guid Id,
    string Name,
    decimal TotalCost,
    decimal SuggestedPrice,
    bool IsActive,
    DateTime ActivityAt);

public sealed record RecentProduct(
    Guid Id,
    string Name,
    decimal PurchasePrice,
    decimal UnitCost,
    bool IsActive,
    DateTime ActivityAt);
