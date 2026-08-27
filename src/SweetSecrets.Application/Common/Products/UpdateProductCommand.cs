namespace SweetSecrets.Application.Common.Products;

public sealed record UpdateProductCommand(
    Guid ProductId,
    string Name,
    decimal PurchaseQuantity,
    Guid UnitId,
    decimal PurchasePrice);