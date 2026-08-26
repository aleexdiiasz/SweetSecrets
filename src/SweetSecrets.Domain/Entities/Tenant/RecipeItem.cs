namespace SweetSecrets.Domain.Entities.Tenant;

public class RecipeItem
{
    public Guid Id { get; set; }

    public Guid RecipeId { get; set; }

    public Guid ProductId { get; set; }

    public decimal Quantity { get; set; }

    public Guid UnitId { get; set; }

    public decimal UnitCost { get; set; }

    public decimal TotalCost { get; set; }

    public Recipe Recipe { get; set; } = null!;

    public Product Product { get; set; } = null!;

    public Unit Unit { get; set; } = null!;
}