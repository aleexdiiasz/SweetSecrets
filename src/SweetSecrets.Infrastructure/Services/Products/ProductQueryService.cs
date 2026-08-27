using Microsoft.EntityFrameworkCore;
using SweetSecrets.Application.Common.Products;
using SweetSecrets.Infrastructure.Data.Tenant;

namespace SweetSecrets.Infrastructure.Services.Products;

public sealed class ProductQueryService : IProductQueryService
{
    private readonly ITenantDbContextFactory _dbContextFactory;

    public ProductQueryService(ITenantDbContextFactory dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    public async Task<IReadOnlyList<ProductListItem>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        await using var dbContext =
            await _dbContextFactory.CreateAsync(
                cancellationToken);

        return await dbContext.Products
            .AsNoTracking()
            .Include(x => x.Unit)
            .OrderBy(x => x.Name)
            .Select(x => new ProductListItem(
                x.Id,
                x.Name,
                x.PurchaseQuantity,
                x.UnitId,
                x.Unit.Code,
                x.Unit.Name,
                x.Unit.Symbol,
                x.PurchasePrice,
                x.UnitCost,
                x.IsActive))
            .ToListAsync(cancellationToken);
    }

    public async Task<ProductDetail?> GetByIdAsync(Guid productId, CancellationToken cancellationToken = default)
    {
        await using var dbContext =
            await _dbContextFactory.CreateAsync(
                cancellationToken);

        return await dbContext.Products
            .AsNoTracking()
            .Where(x => x.Id == productId)
            .Select(x => new ProductDetail(
                x.Id,
                x.Name,
                x.PurchaseQuantity,
                x.UnitId,
                x.Unit.Code,
                x.Unit.Name,
                x.Unit.Symbol,
                x.PurchasePrice,
                x.UnitCost,
                x.IsActive,
                x.CreatedAt,
                x.CreatedBy,
                x.UpdatedAt,
                x.UpdatedBy))
            .FirstOrDefaultAsync(cancellationToken);
    }
}