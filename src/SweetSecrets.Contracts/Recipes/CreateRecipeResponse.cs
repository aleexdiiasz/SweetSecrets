namespace SweetSecrets.Contracts.Recipes;

public sealed class CreateRecipeResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Multiplier { get; set; }
    public decimal TotalCost { get; set; }
    public decimal SuggestedPrice { get; set; }
}