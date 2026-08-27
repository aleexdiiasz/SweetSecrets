namespace SweetSecrets.Application.Common.Products;

public sealed record ProductDetail(
    Guid Id,
    string Name,
    decimal PurchaseQuantity,
    Guid UnitId,
    string UnitCode,
    string UnitName,
    string UnitSymbol,
    decimal PurchasePrice,
    decimal UnitCost,
    bool IsActive,
    DateTime CreatedAt,
    Guid? CreatedBy,
    DateTime? UpdatedAt,
    Guid? UpdatedBy);