namespace SweetSecrets.Contracts.Dashboard;

public sealed class DashboardResponse
{
    public int TotalProducts { get; set; }
    public int ActiveProducts { get; set; }
    public int TotalRecipes { get; set; }
    public int ActiveRecipes { get; set; }
    public decimal AverageActiveRecipeCost { get; set; }
    public IReadOnlyList<RecentRecipeResponse> RecentRecipes { get; set; } = [];
    public IReadOnlyList<RecentProductResponse> RecentProducts { get; set; } = [];
}

public sealed class RecentRecipeResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal TotalCost { get; set; }
    public decimal SuggestedPrice { get; set; }
    public bool IsActive { get; set; }
    public DateTime ActivityAt { get; set; }
}

public sealed class RecentProductResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal PurchasePrice { get; set; }
    public decimal UnitCost { get; set; }
    public bool IsActive { get; set; }
    public DateTime ActivityAt { get; set; }
}
