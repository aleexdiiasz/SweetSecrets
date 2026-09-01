using System.Net.Http.Json;
using SweetSecrets.Contracts.Products;

namespace SweetSecrets.Web.Services.Products;

public sealed class ProductsApiClient
{
    private readonly HttpClient _httpClient;

    public ProductsApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<IReadOnlyList<ProductListItemResponse>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var products =
            await _httpClient
                .GetFromJsonAsync<List<ProductListItemResponse>>(
                    "api/products",
                    cancellationToken);

        return products ?? [];
    }

    public async Task<CreateProductResponse> CreateAsync(CreateProductRequest request, CancellationToken cancellationToken = default)
    {
        var response =
            await _httpClient.PostAsJsonAsync(
                "api/products",
                request,
                cancellationToken);

        response.EnsureSuccessStatusCode();

        var product =
            await response.Content
                .ReadFromJsonAsync<CreateProductResponse>(
                    cancellationToken: cancellationToken);

        if (product is null)
        {
            throw new InvalidOperationException(
                "No fue posible obtener la respuesta del producto creado.");
        }

        return product;
    }

    public async Task<UpdateProductResponse> UpdateAsync(Guid productId, UpdateProductRequest request, CancellationToken cancellationToken = default)
    {
        var response =
            await _httpClient.PutAsJsonAsync(
                $"api/products/{productId}",
                request,
                cancellationToken);

        response.EnsureSuccessStatusCode();

        var product =
            await response.Content
                .ReadFromJsonAsync<UpdateProductResponse>(
                    cancellationToken: cancellationToken);

        if (product is null)
        {
            throw new InvalidOperationException(
                "No fue posible obtener la respuesta del producto actualizado.");
        }

        return product;
    }

    public async Task SetActiveAsync(Guid productId, bool isActive, CancellationToken cancellationToken = default)
    {
        var response =
            await _httpClient.PatchAsJsonAsync(
                $"api/products/{productId}/active",
                new SetProductActiveRequest
                {
                    IsActive = isActive
                },
                cancellationToken);

        response.EnsureSuccessStatusCode();
    }
}