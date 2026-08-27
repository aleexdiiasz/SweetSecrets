namespace SweetSecrets.Contracts.Recipes;

public sealed class RemoveRecipeItemResponse
{
    public Guid RecipeId { get; set; }
    public Guid ItemId { get; set; }
    public decimal RecipeTotalCost { get; set; }
    public decimal RecipeSuggestedPrice { get; set; }
}