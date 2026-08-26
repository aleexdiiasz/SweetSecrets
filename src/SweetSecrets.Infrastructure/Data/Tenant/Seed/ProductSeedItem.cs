namespace SweetSecrets.Infrastructure.Data.Tenant.Seed;

internal class ProductSeedItem
{
    public string Name { get; set; } = string.Empty;

    public decimal PurchaseQuantity { get; set; }

    public string UnitCode { get; set; } = string.Empty;

    public decimal PurchasePrice { get; set; }
}