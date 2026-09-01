using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SweetSecrets.Application.Common.Recipes;
using SweetSecrets.Application.Common.Security;
using SweetSecrets.Contracts.Recipes;

namespace SweetSecrets.Api.Controllers;

[ApiController]
[Route("api/recipes")]
[Authorize(Roles = PlatformRoles.TenantOwner)]
public sealed class RecipesController : ControllerBase
{
    private readonly IRecipeQueryService _recipeQueryService;
    private readonly IRecipeCommandService _recipeCommandService;

    public RecipesController(
        IRecipeQueryService recipeQueryService,
        IRecipeCommandService recipeCommandService)
    {
        _recipeQueryService = recipeQueryService;
        _recipeCommandService = recipeCommandService;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<RecipeListItemResponse>>> GetAll(CancellationToken cancellationToken)
    {
        var recipes = await _recipeQueryService.GetAllAsync(cancellationToken);

        var response = recipes
            .Select(x => new RecipeListItemResponse
            {
                Id = x.Id,
                Name = x.Name,
                Description = x.Description,
                Multiplier = x.Multiplier,
                TotalCost = x.TotalCost,
                SuggestedPrice = x.SuggestedPrice,
                IsActive = x.IsActive
            })
            .ToList();

        return Ok(response);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<RecipeDetailResponse>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var recipe = await _recipeQueryService.GetByIdAsync(
            id,
            cancellationToken);

        if (recipe is null)
            return NotFound();

        return Ok(new RecipeDetailResponse
        {
            Id = recipe.Id,
            Name = recipe.Name,
            Description = recipe.Description,
            Multiplier = recipe.Multiplier,
            TotalCost = recipe.TotalCost,
            SuggestedPrice = recipe.SuggestedPrice,
            IsActive = recipe.IsActive,
            CreatedAt = recipe.CreatedAt,
            CreatedBy = recipe.CreatedBy,
            UpdatedAt = recipe.UpdatedAt,
            UpdatedBy = recipe.UpdatedBy,
            Items = recipe.Items
            .Select(item => new RecipeItemDetailResponse
            {
                Id = item.Id,
                ProductId = item.ProductId,
                ProductName = item.ProductName,
                Quantity = item.Quantity,
                UnitId = item.UnitId,
                UnitCode = item.UnitCode,
                UnitName = item.UnitName,
                UnitSymbol = item.UnitSymbol,
                UnitCost = item.UnitCost,
                TotalCost = item.TotalCost
            })
            .ToList()
        });
    }

    [HttpPost]
    public async Task<ActionResult<CreateRecipeResponse>> Create(CreateRecipeRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _recipeCommandService.CreateAsync(
                new CreateRecipeCommand(
                    request.Name,
                    request.Description,
                    request.Multiplier),
                cancellationToken);

            return Ok(new CreateRecipeResponse
            {
                Id = result.Id,
                Name = result.Name,
                Description = result.Description,
                Multiplier = result.Multiplier,
                TotalCost = result.TotalCost,
                SuggestedPrice = result.SuggestedPrice
            });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    [HttpPost("{id:guid}/items")]
    public async Task<ActionResult<AddRecipeItemResponse>> AddItem(Guid id, AddRecipeItemRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _recipeCommandService.AddItemAsync(
                new AddRecipeItemCommand(
                    id,
                    request.ProductId,
                    request.Quantity,
                    request.UnitId),
                cancellationToken);

            return Ok(new AddRecipeItemResponse
            {
                Id = result.Id,
                RecipeId = result.RecipeId,
                ProductId = result.ProductId,
                Quantity = result.Quantity,
                UnitId = result.UnitId,
                UnitCost = result.UnitCost,
                TotalCost = result.TotalCost
            });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<UpdateRecipeResponse>> Update(Guid id, UpdateRecipeRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _recipeCommandService.UpdateAsync(
                new UpdateRecipeCommand(
                    id,
                    request.Name,
                    request.Description,
                    request.Multiplier),
                cancellationToken);

            if (result is null)
                return NotFound();

            return Ok(new UpdateRecipeResponse
            {
                Id = result.Id,
                Name = result.Name,
                Description = result.Description,
                Multiplier = result.Multiplier,
                TotalCost = result.TotalCost,
                SuggestedPrice = result.SuggestedPrice,
                UpdatedAt = result.UpdatedAt,
                UpdatedBy = result.UpdatedBy
            });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    [HttpPut("{recipeId:guid}/items/{itemId:guid}")]
    public async Task<ActionResult<UpdateRecipeItemResponse>> UpdateItem(Guid recipeId, Guid itemId, UpdateRecipeItemRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _recipeCommandService.UpdateItemAsync(
                new UpdateRecipeItemCommand(
                    recipeId,
                    itemId,
                    request.Quantity),
                cancellationToken);

            if (result is null)
                return NotFound();

            return Ok(new UpdateRecipeItemResponse
            {
                Id = result.Id,
                RecipeId = result.RecipeId,
                ProductId = result.ProductId,
                Quantity = result.Quantity,
                UnitId = result.UnitId,
                UnitCost = result.UnitCost,
                TotalCost = result.TotalCost,
                RecipeTotalCost = result.RecipeTotalCost,
                RecipeSuggestedPrice = result.RecipeSuggestedPrice
            });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    [HttpDelete("{recipeId:guid}/items/{itemId:guid}")]
    public async Task<ActionResult<RemoveRecipeItemResponse>> RemoveItem(Guid recipeId, Guid itemId, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _recipeCommandService.RemoveItemAsync(
                new RemoveRecipeItemCommand(
                    recipeId,
                    itemId),
                cancellationToken);

            if (result is null)
                return NotFound();

            return Ok(new RemoveRecipeItemResponse
            {
                RecipeId = result.RecipeId,
                ItemId = result.ItemId,
                RecipeTotalCost = result.RecipeTotalCost,
                RecipeSuggestedPrice = result.RecipeSuggestedPrice
            });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    [HttpGet("{id:guid}/cost-history")]
    public async Task<ActionResult<IReadOnlyList<RecipeCostHistoryItemResponse>>> GetCostHistory(Guid id, CancellationToken cancellationToken)
    {
        var history = await _recipeQueryService.GetCostHistoryAsync(
            id,
            cancellationToken);

        var response = history
            .Select(x => new RecipeCostHistoryItemResponse
            {
                Id = x.Id,
                RecipeId = x.RecipeId,
                PreviousCost = x.PreviousCost,
                NewCost = x.NewCost,
                Reason = x.Reason,
                CreatedAt = x.CreatedAt
            })
            .ToList();

        return Ok(response);
    }

    [HttpPatch("{id:guid}/active")]
    public async Task<IActionResult> SetActive(Guid id, SetRecipeActiveRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _recipeCommandService.SetActiveAsync(
                id,
                request.IsActive,
                cancellationToken);

            if (!result)
                return NotFound();

            return NoContent();
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
