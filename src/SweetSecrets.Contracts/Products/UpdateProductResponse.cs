namespace SweetSecrets.Contracts.Products;

public sealed class UpdateProductResponse
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public decimal PurchaseQuantity { get; set; }

    public Guid UnitId { get; set; }

    public decimal PurchasePrice { get; set; }

    public decimal UnitCost { get; set; }

    public DateTime UpdatedAt { get; set; }

    public Guid UpdatedBy { get; set; }
}