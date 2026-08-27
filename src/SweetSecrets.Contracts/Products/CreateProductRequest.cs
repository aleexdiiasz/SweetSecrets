namespace SweetSecrets.Contracts.Products;

public sealed class CreateProductRequest
{
    public string Name { get; set; } = string.Empty;

    public decimal PurchaseQuantity { get; set; }

    public Guid UnitId { get; set; }

    public decimal PurchasePrice { get; set; }
}