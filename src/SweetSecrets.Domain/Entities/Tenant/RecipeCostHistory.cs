namespace SweetSecrets.Domain.Entities.Tenant;

public class RecipeCostHistory
{
    public Guid Id { get; set; }

    public Guid RecipeId { get; set; }

    public decimal PreviousCost { get; set; }

    public decimal NewCost { get; set; }

    public string Reason { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public Recipe Recipe { get; set; } = null!;
}