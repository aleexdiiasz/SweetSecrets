using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using SweetSecrets.Application.Common.Recipes;
using SweetSecrets.Application.Common.Tenancy;
using SweetSecrets.Domain.Entities.Tenant;
using SweetSecrets.Infrastructure.Data.Tenant;
using System.Security.Claims;

namespace SweetSecrets.Infrastructure.Services.Recipes;

public sealed class RecipeCommandService : IRecipeCommandService
{
    private readonly ITenantDbContextFactory _dbContextFactory;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public RecipeCommandService(ITenantDbContextFactory dbContextFactory, IHttpContextAccessor httpContextAccessor)
    {
        _dbContextFactory = dbContextFactory;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<CreateRecipeResult> CreateAsync(CreateRecipeCommand command, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(command.Name))
            throw new ArgumentException("El nombre de la receta es obligatorio.");

        var name = command.Name.Trim();

        if (name.Length > 200)
            throw new ArgumentException(
                "El nombre de la receta no puede exceder 200 caracteres.");

        if (command.Multiplier <= 0)
            throw new ArgumentException(
                "El multiplicador debe ser mayor a cero.");

        var userId = GetCurrentUserId();

        await using var dbContext =
            await _dbContextFactory.CreateAsync(cancellationToken);

        var normalizedName = name.ToLower();

        var duplicateExists = await dbContext.Recipes
            .AsNoTracking()
            .AnyAsync(
                x => x.Name.ToLower() == normalizedName,
                cancellationToken);

        if (duplicateExists)
            throw new InvalidOperationException(
                "Ya existe una receta con ese nombre.");

        var now = DateTime.UtcNow;

        var recipe = new Recipe
        {
            Id = Guid.NewGuid(),
            Name = name,
            Description = string.IsNullOrWhiteSpace(command.Description)
                ? null
                : command.Description.Trim(),
            Multiplier = command.Multiplier,
            TotalCost = 0m,
            SuggestedPrice = 0m,
            IsActive = true,
            CreatedAt = now,
            CreatedBy = userId
        };

        dbContext.Recipes.Add(recipe);

        await dbContext.SaveChangesAsync(cancellationToken);

        return new CreateRecipeResult(
            recipe.Id,
            recipe.Name,
            recipe.Description,
            recipe.Multiplier,
            recipe.TotalCost,
            recipe.SuggestedPrice);
    }

    public async Task<AddRecipeItemResult> AddItemAsync(AddRecipeItemCommand command, CancellationToken cancellationToken = default)
    {
        if (command.RecipeId == Guid.Empty)
            throw new ArgumentException("La receta es obligatoria.");

        if (command.ProductId == Guid.Empty)
            throw new ArgumentException("El producto es obligatorio.");

        if (command.UnitId == Guid.Empty)
            throw new ArgumentException("La unidad es obligatoria.");

        if (command.Quantity <= 0)
            throw new ArgumentException(
                "La cantidad debe ser mayor a cero.");

        var userId = GetCurrentUserId();

        await using var dbContext =
            await _dbContextFactory.CreateAsync(cancellationToken);

        var recipe = await dbContext.Recipes
            .Include(x => x.Items)
            .FirstOrDefaultAsync(
                x => x.Id == command.RecipeId,
                cancellationToken);

        if (recipe is null)
            throw new InvalidOperationException(
                "La receta no existe.");

        if (!recipe.IsActive)
            throw new InvalidOperationException(
                "No se pueden agregar ingredientes a una receta inactiva.");

        var product = await dbContext.Products
            .AsNoTracking()
            .FirstOrDefaultAsync(
                x => x.Id == command.ProductId,
                cancellationToken);

        if (product is null)
            throw new InvalidOperationException(
                "El producto no existe.");

        if (!product.IsActive)
            throw new InvalidOperationException(
                "No se puede utilizar un producto inactivo.");

        var unitExists = await dbContext.Units
            .AsNoTracking()
            .AnyAsync(
                x => x.Id == command.UnitId && x.IsActive,
                cancellationToken);

        if (!unitExists)
            throw new InvalidOperationException(
                "La unidad no existe o está inactiva.");

        if (product.UnitId != command.UnitId)
            throw new InvalidOperationException(
                "La unidad del ingrediente debe coincidir con la unidad base del producto.");

        var duplicateProduct = recipe.Items
            .Any(x => x.ProductId == command.ProductId);

        if (duplicateProduct)
            throw new InvalidOperationException(
                "El producto ya forma parte de la receta.");

        var unitCost = product.UnitCost;

        var itemTotalCost = Math.Round(
            command.Quantity * unitCost,
            6,
            MidpointRounding.AwayFromZero);

        var item = new RecipeItem
        {
            Id = Guid.NewGuid(),
            RecipeId = recipe.Id,
            ProductId = product.Id,
            Quantity = command.Quantity,
            UnitId = command.UnitId,
            UnitCost = unitCost,
            TotalCost = itemTotalCost
        };

        recipe.Items.Add(item);

        dbContext.Entry(item).State = EntityState.Added;

        var previousRecipeCost = recipe.TotalCost;

        recipe.TotalCost = Math.Round(
            recipe.Items.Sum(x => x.TotalCost),
            6,
            MidpointRounding.AwayFromZero);

        recipe.SuggestedPrice = Math.Round(
            recipe.TotalCost * recipe.Multiplier,
            2,
            MidpointRounding.AwayFromZero);

        if (previousRecipeCost != recipe.TotalCost)
        {
            dbContext.RecipeCostHistory.Add(new RecipeCostHistory
            {
                Id = Guid.NewGuid(),
                RecipeId = recipe.Id,
                PreviousCost = previousRecipeCost,
                NewCost = recipe.TotalCost,
                Reason = "RECIPE_ITEM_ADDED",
                CreatedAt = DateTime.UtcNow
            });
        }

        recipe.UpdatedAt = DateTime.UtcNow;
        recipe.UpdatedBy = userId;

        await dbContext.SaveChangesAsync(cancellationToken);

        return new AddRecipeItemResult(
            item.Id,
            item.RecipeId,
            item.ProductId,
            item.Quantity,
            item.UnitId,
            item.UnitCost,
            item.TotalCost);
    }

    public async Task<UpdateRecipeResult?> UpdateAsync(UpdateRecipeCommand command, CancellationToken cancellationToken = default)
    {
        if (command.RecipeId == Guid.Empty)
            throw new ArgumentException("La receta es obligatoria.");

        if (string.IsNullOrWhiteSpace(command.Name))
            throw new ArgumentException("El nombre de la receta es obligatorio.");

        var name = command.Name.Trim();

        if (name.Length > 200)
            throw new ArgumentException(
                "El nombre de la receta no puede exceder 200 caracteres.");

        if (command.Multiplier <= 0)
            throw new ArgumentException(
                "El multiplicador debe ser mayor a cero.");

        var userId = GetCurrentUserId();

        await using var dbContext =
            await _dbContextFactory.CreateAsync(cancellationToken);

        var recipe = await dbContext.Recipes
            .FirstOrDefaultAsync(
                x => x.Id == command.RecipeId,
                cancellationToken);

        if (recipe is null)
            return null;

        var normalizedName = name.ToLower();

        var duplicateExists = await dbContext.Recipes
            .AsNoTracking()
            .AnyAsync(
                x => x.Id != command.RecipeId &&
                     x.Name.ToLower() == normalizedName,
                cancellationToken);

        if (duplicateExists)
            throw new InvalidOperationException(
                "Ya existe otra receta con ese nombre.");

        recipe.Name = name;
        recipe.Description = string.IsNullOrWhiteSpace(command.Description)
            ? null
            : command.Description.Trim();

        recipe.Multiplier = command.Multiplier;

        recipe.SuggestedPrice = Math.Round(
            recipe.TotalCost * recipe.Multiplier,
            2,
            MidpointRounding.AwayFromZero);

        var updatedAt = DateTime.UtcNow;

        recipe.UpdatedAt = updatedAt;
        recipe.UpdatedBy = userId;

        await dbContext.SaveChangesAsync(cancellationToken);

        return new UpdateRecipeResult(
            recipe.Id,
            recipe.Name,
            recipe.Description,
            recipe.Multiplier,
            recipe.TotalCost,
            recipe.SuggestedPrice,
            updatedAt,
            userId);
    }

    public async Task<UpdateRecipeItemResult?> UpdateItemAsync(UpdateRecipeItemCommand command, CancellationToken cancellationToken = default)
    {
        if (command.RecipeId == Guid.Empty)
            throw new ArgumentException("La receta es obligatoria.");

        if (command.ItemId == Guid.Empty)
            throw new ArgumentException("El ingrediente es obligatorio.");

        if (command.Quantity <= 0)
            throw new ArgumentException(
                "La cantidad debe ser mayor a cero.");

        var userId = GetCurrentUserId();

        await using var dbContext =
            await _dbContextFactory.CreateAsync(cancellationToken);

        var recipe = await dbContext.Recipes
            .Include(x => x.Items)
            .FirstOrDefaultAsync(
                x => x.Id == command.RecipeId,
                cancellationToken);

        if (recipe is null)
            return null;

        if (!recipe.IsActive)
            throw new InvalidOperationException(
                "No se puede modificar una receta inactiva.");

        var item = recipe.Items
            .FirstOrDefault(x => x.Id == command.ItemId);

        if (item is null)
            return null;

        var previousRecipeCost = recipe.TotalCost;

        item.Quantity = command.Quantity;

        item.TotalCost = Math.Round(
            item.Quantity * item.UnitCost,
            6,
            MidpointRounding.AwayFromZero);

        recipe.TotalCost = Math.Round(
            recipe.Items.Sum(x => x.TotalCost),
            6,
            MidpointRounding.AwayFromZero);

        recipe.SuggestedPrice = Math.Round(
            recipe.TotalCost * recipe.Multiplier,
            2,
            MidpointRounding.AwayFromZero);

        if (previousRecipeCost != recipe.TotalCost)
        {
            dbContext.RecipeCostHistory.Add(new RecipeCostHistory
            {
                Id = Guid.NewGuid(),
                RecipeId = recipe.Id,
                PreviousCost = previousRecipeCost,
                NewCost = recipe.TotalCost,
                Reason = "RECIPE_ITEM_UPDATED",
                CreatedAt = DateTime.UtcNow
            });
        }

        var updatedAt = DateTime.UtcNow;

        recipe.UpdatedAt = updatedAt;
        recipe.UpdatedBy = userId;

        await dbContext.SaveChangesAsync(cancellationToken);

        return new UpdateRecipeItemResult(
            item.Id,
            recipe.Id,
            item.ProductId,
            item.Quantity,
            item.UnitId,
            item.UnitCost,
            item.TotalCost,
            recipe.TotalCost,
            recipe.SuggestedPrice);
    }

    public async Task<RemoveRecipeItemResult?> RemoveItemAsync(RemoveRecipeItemCommand command, CancellationToken cancellationToken = default)
    {
        if (command.RecipeId == Guid.Empty)
            throw new ArgumentException("La receta es obligatoria.");

        if (command.ItemId == Guid.Empty)
            throw new ArgumentException("El ingrediente es obligatorio.");

        var userId = GetCurrentUserId();

        await using var dbContext =
            await _dbContextFactory.CreateAsync(cancellationToken);

        var recipe = await dbContext.Recipes
            .Include(x => x.Items)
            .FirstOrDefaultAsync(
                x => x.Id == command.RecipeId,
                cancellationToken);

        if (recipe is null)
            return null;

        if (!recipe.IsActive)
            throw new InvalidOperationException(
                "No se puede modificar una receta inactiva.");

        var item = recipe.Items
            .FirstOrDefault(x => x.Id == command.ItemId);

        if (item is null)
            return null;

        var previousRecipeCost = recipe.TotalCost;

        dbContext.RecipeItems.Remove(item);

        recipe.Items.Remove(item);

        recipe.TotalCost = Math.Round(
            recipe.Items.Sum(x => x.TotalCost),
            6,
            MidpointRounding.AwayFromZero);

        recipe.SuggestedPrice = Math.Round(
            recipe.TotalCost * recipe.Multiplier,
            2,
            MidpointRounding.AwayFromZero);

        if (previousRecipeCost != recipe.TotalCost)
        {
            dbContext.RecipeCostHistory.Add(new RecipeCostHistory
            {
                Id = Guid.NewGuid(),
                RecipeId = recipe.Id,
                PreviousCost = previousRecipeCost,
                NewCost = recipe.TotalCost,
                Reason = "RECIPE_ITEM_REMOVED",
                CreatedAt = DateTime.UtcNow
            });
        }

        var updatedAt = DateTime.UtcNow;

        recipe.UpdatedAt = updatedAt;
        recipe.UpdatedBy = userId;

        await dbContext.SaveChangesAsync(cancellationToken);

        return new RemoveRecipeItemResult(
            recipe.Id,
            item.Id,
            recipe.TotalCost,
            recipe.SuggestedPrice);
    }

    public async Task<bool> SetActiveAsync(Guid recipeId, bool isActive, CancellationToken cancellationToken = default)
    {
        if (recipeId == Guid.Empty)
            throw new ArgumentException("La receta es obligatoria.");

        var userId = GetCurrentUserId();

        await using var dbContext =
            await _dbContextFactory.CreateAsync(cancellationToken);

        var recipe = await dbContext.Recipes
            .Include(x => x.Items)
            .ThenInclude(x => x.Product)
            .FirstOrDefaultAsync(
            x => x.Id == recipeId,
            cancellationToken);

        if (recipe is null)
            return false;

        if (recipe.IsActive == isActive)
            return true;

        if (isActive)
        {
            var previousRecipeCost =
                recipe.TotalCost;

            foreach (var item in recipe.Items)
            {
                item.UnitCost =
                    item.Product.UnitCost;

                item.TotalCost =
                    Math.Round(
                        item.Quantity * item.UnitCost,
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

            if (previousRecipeCost != recipe.TotalCost)
            {
                var history =
                    new RecipeCostHistory
                    {
                        Id = Guid.NewGuid(),
                        RecipeId = recipe.Id,
                        PreviousCost = previousRecipeCost,
                        NewCost = recipe.TotalCost,
                        Reason = "RECIPE_REACTIVATED_COST_SYNC",
                        CreatedAt = DateTime.UtcNow
                    };

                await dbContext.RecipeCostHistory.AddAsync(
                    history,
                    cancellationToken);
            }
        }

        recipe.IsActive = isActive;
        recipe.UpdatedAt = DateTime.UtcNow;
        recipe.UpdatedBy = userId;

        await dbContext.SaveChangesAsync(cancellationToken);

        return true;
    }

    private Guid GetCurrentUserId()
    {
        var value = _httpContextAccessor
            .HttpContext?
            .User
            .FindFirstValue(ClaimTypes.NameIdentifier);

        if (!Guid.TryParse(value, out var userId))
            throw new InvalidOperationException(
                "No se pudo identificar al usuario autenticado.");

        return userId;
    }
}