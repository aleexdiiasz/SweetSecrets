namespace SweetSecrets.Contracts.Products;

public sealed class ProductListItemResponse
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public decimal PurchaseQuantity { get; set; }

    public Guid UnitId { get; set; }

    public string UnitCode { get; set; } = string.Empty;

    public string UnitName { get; set; } = string.Empty;

    public string UnitSymbol { get; set; } = string.Empty;

    public decimal PurchasePrice { get; set; }

    public decimal UnitCost { get; set; }

    public bool IsActive { get; set; }
}