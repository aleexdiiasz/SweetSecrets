namespace SweetSecrets.Application.Common.Recipes;

public sealed record RecipeItemDetail(
    Guid Id,
    Guid ProductId,
    string ProductName,
    decimal Quantity,
    Guid UnitId,
    string UnitCode,
    string UnitName,
    string UnitSymbol,
    decimal UnitCost,
    decimal TotalCost);