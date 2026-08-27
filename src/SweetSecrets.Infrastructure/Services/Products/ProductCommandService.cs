using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using SweetSecrets.Application.Common.Products;
using SweetSecrets.Domain.Entities.Tenant;
using SweetSecrets.Infrastructure.Data.Tenant;

namespace SweetSecrets.Infrastructure.Services.Products;

public sealed class ProductCommandService : IProductCommandService
{
    private readonly ITenantDbContextFactory _dbContextFactory;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public ProductCommandService(ITenantDbContextFactory dbContextFactory, IHttpContextAccessor httpContextAccessor)
    {
        _dbContextFactory = dbContextFactory;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<CreateProductResult> CreateAsync(CreateProductCommand command, CancellationToken cancellationToken = default)
    {
        Validate(command);

        var userId = GetCurrentUserId();

        await using var dbContext =
            await _dbContextFactory.CreateAsync(
                cancellationToken);

        var unitExists =
            await dbContext.Units
                .AsNoTracking()
                .AnyAsync(
                    x =>
                        x.Id == command.UnitId &&
                        x.IsActive,
                    cancellationToken);

        if (!unitExists)
        {
            throw new InvalidOperationException(
                "La unidad seleccionada no existe o está inactiva.");
        }

        var normalizedName =
            command.Name.Trim();

        var duplicateExists =
            await dbContext.Products
                .AsNoTracking()
                .AnyAsync(
                    x =>
                        x.Name.ToLower() ==
                        normalizedName.ToLower(),
                    cancellationToken);

        if (duplicateExists)
        {
            throw new InvalidOperationException(
                "Ya existe un producto con ese nombre.");
        }

        var unitCost =
            Math.Round(
                command.PurchasePrice /
                command.PurchaseQuantity,
                6,
                MidpointRounding.AwayFromZero);

        var product =
            new Product
            {
                Id = Guid.NewGuid(),

                Name = normalizedName,

                PurchaseQuantity = command.PurchaseQuantity,

                UnitId = command.UnitId,

                PurchasePrice = command.PurchasePrice,

                UnitCost = unitCost,

                IsActive = true,

                CreatedAt = DateTime.UtcNow,

                CreatedBy = userId
            };

        await dbContext.Products.AddAsync(product, cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);

        return new CreateProductResult(
            product.Id,
            product.Name,
            product.PurchaseQuantity,
            product.UnitId,
            product.PurchasePrice,
            product.UnitCost);
    }

    public async Task<UpdateProductResult?> UpdateAsync(UpdateProductCommand command, CancellationToken cancellationToken = default)
    {
        ValidateUpdate(command);

        var userId = GetCurrentUserId();

        await using var dbContext =
            await _dbContextFactory.CreateAsync(
                cancellationToken);

        var product =
    await dbContext.Products
        .Include(x => x.Unit)
        .FirstOrDefaultAsync(
            x => x.Id == command.ProductId,
            cancellationToken);

        if (product is null)
        {
            return null;
        }

        var targetUnit =
    await dbContext.Units
        .AsNoTracking()
        .FirstOrDefaultAsync(
            x =>
                x.Id == command.UnitId &&
                x.IsActive,
            cancellationToken);

        if (targetUnit is null)
        {
            throw new InvalidOperationException(
                "La unidad seleccionada no existe o está inactiva.");
        }

        var normalizedName =
            command.Name.Trim();

        var duplicateExists =
            await dbContext.Products
                .AsNoTracking()
                .AnyAsync(
                    x =>
                        x.Id != command.ProductId &&
                        x.Name.ToLower() ==
                        normalizedName.ToLower(),
                    cancellationToken);

        if (duplicateExists)
        {
            throw new InvalidOperationException(
                "Ya existe otro producto con ese nombre.");
        }

        var previousPrice = product.PurchasePrice;

        var previousUnitCost = product.UnitCost;

        var previousUnitId = product.UnitId;

        var unitCost =
            Math.Round(
                command.PurchasePrice /
                command.PurchaseQuantity,
                6,
                MidpointRounding.AwayFromZero);

        var unitChanged = previousUnitId != command.UnitId;

        if (unitChanged)
        {
            if (targetUnit.ConversionFactor <= 0)
            {
                throw new InvalidOperationException(
                    "La configuración de conversión de la unidad seleccionada no es válida.");
            }

            var productIsUsedInRecipes =
                await dbContext.RecipeItems
                    .AsNoTracking()
                    .AnyAsync(
                        x => x.ProductId == product.Id,
                        cancellationToken);

            if (productIsUsedInRecipes &&
                product.Unit.MeasurementType != targetUnit.MeasurementType)
            {
                throw new InvalidOperationException(
                    "No se puede cambiar la unidad del producto porque no es compatible con las unidades utilizadas en sus recetas.");
            }
        }



        var priceChanged = previousPrice != command.PurchasePrice || previousUnitCost != unitCost;

        var unitCostChanged = previousUnitCost != unitCost;

        var updatedAt = DateTime.UtcNow;

        product.Name = normalizedName;

        product.PurchaseQuantity = command.PurchaseQuantity;

        product.UnitId = command.UnitId;

        product.PurchasePrice = command.PurchasePrice;

        product.UnitCost = unitCost;

        product.UpdatedAt = updatedAt;

        product.UpdatedBy = userId;

        if (priceChanged)
        {
            var history =
                new ProductPriceHistory
                {
                    Id = Guid.NewGuid(),

                    ProductId = product.Id,

                    PreviousPrice = previousPrice,

                    NewPrice = command.PurchasePrice,

                    PreviousUnitCost = previousUnitCost,

                    NewUnitCost = unitCost,

                    ChangedBy = userId,

                    ChangedAt = updatedAt
                };

            await dbContext.ProductPriceHistory.AddAsync(
                history,
                cancellationToken);
        }

        if (unitCostChanged || unitChanged)
        {
            var affectedRecipes =
                await dbContext.Recipes
                    .Include(x => x.Items)
                    .ThenInclude(x => x.Unit)
                    .Where(x =>
                        x.IsActive &&
                        x.Items.Any(item =>
                            item.ProductId == product.Id))
                    .ToListAsync(cancellationToken);

            foreach (var recipe in affectedRecipes)
            {
                var previousRecipeCost = recipe.TotalCost;

                foreach (var item in recipe.Items.Where(x => x.ProductId == product.Id))
                {
                    if (item.Unit.MeasurementType != targetUnit.MeasurementType)
                    {
                        throw new InvalidOperationException(
                            "La unidad del ingrediente no es compatible con la unidad del producto.");
                    }

                    if (targetUnit.ConversionFactor <= 0 || item.Unit.ConversionFactor <= 0)
                    {
                        throw new InvalidOperationException(
                            "La configuración de conversión de unidades no es válida.");
                    }

                    var convertedUnitCost =
                        Math.Round(
                            unitCost *
                            item.Unit.ConversionFactor /
                            targetUnit.ConversionFactor,
                            6,
                            MidpointRounding.AwayFromZero);

                    item.UnitCost =
                        convertedUnitCost;

                    item.TotalCost =
                        Math.Round(
                            item.Quantity * convertedUnitCost,
                            6,
                            MidpointRounding.AwayFromZero);
                }

                recipe.TotalCost =
                    Math.Round(
                        recipe.Items.Sum(x => x.TotalCost),
                        6,
                        MidpointRounding.AwayFromZero);

                recipe.SuggestedPrice =
                    Math.Round(
                        recipe.TotalCost * recipe.Multiplier,
                        2,
                        MidpointRounding.AwayFromZero);

                recipe.UpdatedAt = updatedAt;

                recipe.UpdatedBy = userId;

                if (previousRecipeCost != recipe.TotalCost)
                {
                    var recipeHistory =
                        new RecipeCostHistory
                        {
                            Id = Guid.NewGuid(),
                            RecipeId = recipe.Id,
                            PreviousCost = previousRecipeCost,
                            NewCost = recipe.TotalCost,
                            Reason = unitChanged ? "PRODUCT_UNIT_CHANGED" : "PRODUCT_UNIT_COST_CHANGED",
                            CreatedAt = updatedAt
                        };

                    await dbContext.RecipeCostHistory.AddAsync(
                        recipeHistory,
                        cancellationToken);
                }
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return new UpdateProductResult(
            product.Id,
            product.Name,
            product.PurchaseQuantity,
            product.UnitId,
            product.PurchasePrice,
            product.UnitCost,
            updatedAt,
            userId);
    }

    public async Task<bool> SetActiveAsync(Guid productId, bool isActive, CancellationToken cancellationToken = default)
    {
        if (productId == Guid.Empty)
        {
            throw new ArgumentException(
                "El producto es obligatorio.");
        }

        var userId = GetCurrentUserId();

        await using var dbContext =
            await _dbContextFactory.CreateAsync(
                cancellationToken);

        var product =
            await dbContext.Products
                .FirstOrDefaultAsync(
                    x => x.Id == productId,
                    cancellationToken);

        if (product is null)
        {
            return false;
        }

        if (product.IsActive == isActive)
        {
            return true;
        }

        product.IsActive = isActive;

        product.UpdatedAt = DateTime.UtcNow;

        product.UpdatedBy = userId;

        await dbContext.SaveChangesAsync(cancellationToken);

        return true;
    }

    private Guid GetCurrentUserId()
    {
        var userIdValue =
            _httpContextAccessor
                .HttpContext?
                .User
                .FindFirstValue(
                    ClaimTypes.NameIdentifier);

        if (!Guid.TryParse(
                userIdValue,
                out var userId))
        {
            throw new UnauthorizedAccessException(
                "No fue posible identificar al usuario autenticado.");
        }

        return userId;
    }

    private static void Validate(CreateProductCommand command)
    {
        if (string.IsNullOrWhiteSpace(command.Name))
        {
            throw new ArgumentException(
                "El nombre del producto es obligatorio.");
        }

        if (command.Name.Trim().Length > 200)
        {
            throw new ArgumentException(
                "El nombre del producto no puede superar 200 caracteres.");
        }

        if (command.PurchaseQuantity <= 0)
        {
            throw new ArgumentException(
                "La cantidad de compra debe ser mayor a cero.");
        }

        if (command.UnitId == Guid.Empty)
        {
            throw new ArgumentException(
                "La unidad es obligatoria.");
        }

        if (command.PurchasePrice < 0)
        {
            throw new ArgumentException(
                "El precio de compra no puede ser negativo.");
        }
    }

    private static void ValidateUpdate(UpdateProductCommand command)
    {
        if (command.ProductId == Guid.Empty)
        {
            throw new ArgumentException(
                "El producto es obligatorio.");
        }

        if (string.IsNullOrWhiteSpace(
                command.Name))
        {
            throw new ArgumentException(
                "El nombre del producto es obligatorio.");
        }

        if (command.Name.Trim().Length > 200)
        {
            throw new ArgumentException(
                "El nombre del producto no puede superar 200 caracteres.");
        }

        if (command.PurchaseQuantity <= 0)
        {
            throw new ArgumentException(
                "La cantidad de compra debe ser mayor a cero.");
        }

        if (command.UnitId == Guid.Empty)
        {
            throw new ArgumentException(
                "La unidad es obligatoria.");
        }

        if (command.PurchasePrice < 0)
        {
            throw new ArgumentException(
                "El precio de compra no puede ser negativo.");
        }
    }
}