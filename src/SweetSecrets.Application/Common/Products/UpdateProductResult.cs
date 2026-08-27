namespace SweetSecrets.Application.Common.Products;

public sealed record UpdateProductResult(
    Guid Id,
    string Name,
    decimal PurchaseQuantity,
    Guid UnitId,
    decimal PurchasePrice,
    decimal UnitCost,
    DateTime UpdatedAt,
    Guid UpdatedBy);