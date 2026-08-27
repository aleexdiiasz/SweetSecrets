namespace SweetSecrets.Contracts.Recipes;

public sealed class RecipeDetailResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Multiplier { get; set; }
    public decimal TotalCost { get; set; }
    public decimal SuggestedPrice { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public Guid? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public Guid? UpdatedBy { get; set; }
    public IReadOnlyList<RecipeItemDetailResponse> Items { get; set; }
    = Array.Empty<RecipeItemDetailResponse>();
}