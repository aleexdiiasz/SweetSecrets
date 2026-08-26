namespace SweetSecrets.Domain.Entities.Tenant;

public class Recipe
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public decimal Multiplier { get; set; }

    public decimal TotalCost { get; set; }

    public decimal SuggestedPrice { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; }

    public Guid? CreatedBy { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public Guid? UpdatedBy { get; set; }

    public ICollection<RecipeItem> Items { get; set; } = [];
}