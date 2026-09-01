using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SweetSecrets.Application.Common.Products;
using SweetSecrets.Contracts.Products;
using SweetSecrets.Infrastructure.Services.Products;
using SweetSecrets.Application.Common.Security;

namespace SweetSecrets.Api.Controllers;

[ApiController]
[Route("api/products")]
[Authorize(Roles = PlatformRoles.TenantOwner)]
public sealed class ProductsController : ControllerBase
{
    private readonly IProductQueryService _productQueryService;

    private readonly IProductCommandService _productCommandService;

    public ProductsController(IProductQueryService productQueryService, IProductCommandService productCommandService)
    {
        _productQueryService = productQueryService;
        _productCommandService = productCommandService;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ProductListItemResponse>>> GetAll(CancellationToken cancellationToken)
    {
        var products =
            await _productQueryService.GetAllAsync(
                cancellationToken);

        var response =
            products
                .Select(x => new ProductListItemResponse
                {
                    Id = x.Id,
                    Name = x.Name,
                    PurchaseQuantity = x.PurchaseQuantity,
                    UnitId = x.UnitId,
                    UnitCode = x.UnitCode,
                    UnitName = x.UnitName,
                    UnitSymbol = x.UnitSymbol,
                    PurchasePrice = x.PurchasePrice,
                    UnitCost = x.UnitCost,
                    IsActive = x.IsActive
                })
                .ToList();

        return Ok(response);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ProductDetailResponse>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var product =
            await _productQueryService.GetByIdAsync(
                id,
                cancellationToken);

        if (product is null)
        {
            return NotFound();
        }

        return Ok(
            new ProductDetailResponse
            {
                Id = product.Id,
                Name = product.Name,
                PurchaseQuantity = product.PurchaseQuantity,
                UnitId = product.UnitId,
                UnitCode = product.UnitCode,
                UnitName = product.UnitName,
                UnitSymbol = product.UnitSymbol,
                PurchasePrice = product.PurchasePrice,
                UnitCost = product.UnitCost,
                IsActive = product.IsActive,
                CreatedAt = product.CreatedAt,
                CreatedBy = product.CreatedBy,
                UpdatedAt = product.UpdatedAt,
                UpdatedBy = product.UpdatedBy
            });
    }

    [HttpPost]
    public async Task<ActionResult<CreateProductResponse>> Create(CreateProductRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var result =
                await _productCommandService.CreateAsync(
                    new CreateProductCommand(
                        request.Name,
                        request.PurchaseQuantity,
                        request.UnitId,
                        request.PurchasePrice),
                    cancellationToken);

            return Ok(
                new CreateProductResponse
                {
                    Id = result.Id,
                    Name = result.Name,
                    PurchaseQuantity = result.PurchaseQuantity,
                    UnitId = result.UnitId,
                    PurchasePrice = result.PurchasePrice,
                    UnitCost = result.UnitCost
                });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(
                new
                {
                    message = ex.Message
                });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(
                new
                {
                    message = ex.Message
                });
        }
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<UpdateProductResponse>> Update(Guid id, UpdateProductRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var result =
                await _productCommandService.UpdateAsync(
                    new UpdateProductCommand(
                        id,
                        request.Name,
                        request.PurchaseQuantity,
                        request.UnitId,
                        request.PurchasePrice),
                    cancellationToken);

            if (result is null)
            {
                return NotFound();
            }

            return Ok(
                new UpdateProductResponse
                {
                    Id = result.Id,
                    Name = result.Name,
                    PurchaseQuantity = result.PurchaseQuantity,
                    UnitId = result.UnitId,
                    PurchasePrice = result.PurchasePrice,
                    UnitCost = result.UnitCost,
                    UpdatedAt = result.UpdatedAt,
                    UpdatedBy = result.UpdatedBy
                });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new
            {
                message = ex.Message
            });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new
            {
                message = ex.Message
            });
        }
    }

    [HttpPatch("{id:guid}/active")]
    public async Task<IActionResult> SetActive(Guid id, SetProductActiveRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var result =
                await _productCommandService.SetActiveAsync(
                    id,
                    request.IsActive,
                    cancellationToken);

            if (!result)
            {
                return NotFound();
            }

            return NoContent();
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new
            {
                message = ex.Message
            });
        }
    }
}
