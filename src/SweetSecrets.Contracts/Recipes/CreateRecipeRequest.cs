namespace SweetSecrets.Contracts.Recipes;

public sealed class CreateRecipeRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Multiplier { get; set; }
}