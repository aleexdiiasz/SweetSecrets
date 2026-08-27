namespace SweetSecrets.Application.Common.Products;

public interface IProductCommandService
{
    Task<CreateProductResult> CreateAsync(
        CreateProductCommand command,
        CancellationToken cancellationToken = default);

    Task<UpdateProductResult?> UpdateAsync(
        UpdateProductCommand command,
        CancellationToken cancellationToken = default);

    Task<bool> SetActiveAsync(
    Guid productId,
    bool isActive,
    CancellationToken cancellationToken = default);
}