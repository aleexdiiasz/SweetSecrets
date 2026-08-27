namespace SweetSecrets.Application.Common.Products;

public sealed record CreateProductCommand(
    string Name,
    decimal PurchaseQuantity,
    Guid UnitId,
    decimal PurchasePrice);