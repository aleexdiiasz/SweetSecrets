namespace SweetSecrets.Domain.Entities.Tenant;

public class ProductPriceHistory
{
    public Guid Id { get; set; }

    public Guid ProductId { get; set; }

    public decimal PreviousPrice { get; set; }

    public decimal NewPrice { get; set; }

    public decimal PreviousUnitCost { get; set; }

    public decimal NewUnitCost { get; set; }

    public Guid? ChangedBy { get; set; }

    public DateTime ChangedAt { get; set; }

    public Product Product { get; set; } = null!;
}