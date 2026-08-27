namespace SweetSecrets.Contracts.Recipes;

public sealed class UpdateRecipeItemResponse
{
    public Guid Id { get; set; }
    public Guid RecipeId { get; set; }
    public Guid ProductId { get; set; }
    public decimal Quantity { get; set; }
    public Guid UnitId { get; set; }
    public decimal UnitCost { get; set; }
    public decimal TotalCost { get; set; }
    public decimal RecipeTotalCost { get; set; }
    public decimal RecipeSuggestedPrice { get; set; }
}