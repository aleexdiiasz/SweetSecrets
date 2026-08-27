namespace SweetSecrets.Application.Common.Products;

public sealed record CreateProductResult(
    Guid Id,
    string Name,
    decimal PurchaseQuantity,
    Guid UnitId,
    decimal PurchasePrice,
    decimal UnitCost);