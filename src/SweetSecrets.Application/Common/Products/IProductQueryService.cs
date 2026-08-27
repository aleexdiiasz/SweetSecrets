namespace SweetSecrets.Application.Common.Products;

public interface IProductQueryService
{
    Task<IReadOnlyList<ProductListItem>> GetAllAsync(
        CancellationToken cancellationToken = default);

    Task<ProductDetail?> GetByIdAsync(
        Guid productId,
        CancellationToken cancellationToken = default);
}