namespace SweetSecrets.Contracts.Products;

public sealed class ProductDetailResponse
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

    public DateTime CreatedAt { get; set; }

    public Guid? CreatedBy { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public Guid? UpdatedBy { get; set; }
}