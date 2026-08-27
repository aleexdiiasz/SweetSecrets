namespace SweetSecrets.Contracts.Recipes;

public sealed class AddRecipeItemRequest
{
    public Guid ProductId { get; set; }
    public decimal Quantity { get; set; }
    public Guid UnitId { get; set; }
}