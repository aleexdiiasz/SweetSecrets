namespace SweetSecrets.Contracts.Recipes;

public sealed class RecipeCostHistoryItemResponse
{
    public Guid Id { get; set; }
    public Guid RecipeId { get; set; }
    public decimal PreviousCost { get; set; }
    public decimal NewCost { get; set; }
    public string Reason { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}