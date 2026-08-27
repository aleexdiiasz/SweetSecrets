namespace SweetSecrets.Application.Common.Products;

public sealed record ProductListItem(
    Guid Id,
    string Name,
    decimal PurchaseQuantity,
    Guid UnitId,
    string UnitCode,
    string UnitName,
    string UnitSymbol,
    decimal PurchasePrice,
    decimal UnitCost,
    bool IsActive);