using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using SweetSecrets.Contracts.Recipes;

namespace SweetSecrets.Web.Services.Recipes;

public sealed class RecipesApiClient
{
    private readonly HttpClient _httpClient;

    public RecipesApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<IReadOnlyList<RecipeListItemResponse>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.GetAsync("api/recipes", cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);

        return await response.Content.ReadFromJsonAsync<List<RecipeListItemResponse>>(cancellationToken)
            ?? [];
    }

    public async Task<RecipeDetailResponse> GetByIdAsync(Guid recipeId, CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.GetAsync($"api/recipes/{recipeId}", cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);

        return await response.Content.ReadFromJsonAsync<RecipeDetailResponse>(cancellationToken)
            ?? throw new InvalidOperationException("No fue posible obtener el detalle de la receta.");
    }

    public async Task<CreateRecipeResponse> CreateAsync(CreateRecipeRequest request, CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.PostAsJsonAsync("api/recipes", request, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);

        return await response.Content.ReadFromJsonAsync<CreateRecipeResponse>(cancellationToken)
            ?? throw new InvalidOperationException("No fue posible obtener la receta creada.");
    }

    public async Task<UpdateRecipeResponse> UpdateAsync(Guid recipeId, UpdateRecipeRequest request, CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.PutAsJsonAsync($"api/recipes/{recipeId}", request, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);

        return await response.Content.ReadFromJsonAsync<UpdateRecipeResponse>(cancellationToken)
            ?? throw new InvalidOperationException("No fue posible obtener la receta actualizada.");
    }

    public async Task<AddRecipeItemResponse> AddItemAsync(Guid recipeId, AddRecipeItemRequest request, CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.PostAsJsonAsync($"api/recipes/{recipeId}/items", request, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);

        return await response.Content.ReadFromJsonAsync<AddRecipeItemResponse>(cancellationToken)
            ?? throw new InvalidOperationException("No fue posible obtener el ingrediente agregado.");
    }

    public async Task<UpdateRecipeItemResponse> UpdateItemAsync(Guid recipeId, Guid itemId, UpdateRecipeItemRequest request, CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.PutAsJsonAsync($"api/recipes/{recipeId}/items/{itemId}", request, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);

        return await response.Content.ReadFromJsonAsync<UpdateRecipeItemResponse>(cancellationToken)
            ?? throw new InvalidOperationException("No fue posible obtener el ingrediente actualizado.");
    }

    public async Task<RemoveRecipeItemResponse> RemoveItemAsync(Guid recipeId, Guid itemId, CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.DeleteAsync($"api/recipes/{recipeId}/items/{itemId}", cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);

        return await response.Content.ReadFromJsonAsync<RemoveRecipeItemResponse>(cancellationToken)
            ?? throw new InvalidOperationException("No fue posible obtener el resultado de eliminar el ingrediente.");
    }

    public async Task<IReadOnlyList<RecipeCostHistoryItemResponse>> GetCostHistoryAsync(Guid recipeId, CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.GetAsync($"api/recipes/{recipeId}/cost-history", cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);

        return await response.Content.ReadFromJsonAsync<List<RecipeCostHistoryItemResponse>>(cancellationToken)
            ?? [];
    }

    public async Task SetActiveAsync(Guid recipeId, bool isActive, CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.PatchAsJsonAsync(
            $"api/recipes/{recipeId}/active",
            new SetRecipeActiveRequest { IsActive = isActive },
            cancellationToken);

        await EnsureSuccessAsync(response, cancellationToken);
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
            return;

        var message = response.StatusCode switch
        {
            HttpStatusCode.BadRequest => "La información enviada no es válida.",
            HttpStatusCode.Unauthorized => "Tu sesión terminó. Inicia sesión nuevamente.",
            HttpStatusCode.Forbidden => "No tienes permiso para realizar esta operación.",
            HttpStatusCode.NotFound => "La receta o ingrediente ya no existe.",
            HttpStatusCode.Conflict => "La operación entra en conflicto con el estado actual de la receta.",
            _ => "No fue posible completar la operación."
        };

        try
        {
            using var payload = await JsonDocument.ParseAsync(
                await response.Content.ReadAsStreamAsync(cancellationToken),
                cancellationToken: cancellationToken);

            if (payload.RootElement.TryGetProperty("message", out var value) &&
                !string.IsNullOrWhiteSpace(value.GetString()))
            {
                message = value.GetString()!;
            }
        }
        catch (JsonException)
        {
            // Preserve the status-specific fallback for non-JSON error responses.
        }

        throw new RecipesApiException(response.StatusCode, message);
    }
}

public sealed class RecipesApiException : HttpRequestException
{
    public RecipesApiException(HttpStatusCode statusCode, string message)
        : base(message, null, statusCode)
    {
    }
}
