using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using SweetSecrets.Contracts.Settings;

namespace SweetSecrets.Web.Services.Settings;

public sealed class SettingsApiClient
{
    private readonly HttpClient _httpClient;

    public SettingsApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<IReadOnlyList<SettingListItemResponse>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.GetAsync("api/settings", cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);

        return await response.Content.ReadFromJsonAsync<List<SettingListItemResponse>>(cancellationToken)
            ?? [];
    }

    public async Task<SettingDetailResponse> GetByKeyAsync(string key, CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.GetAsync(
            $"api/settings/{Uri.EscapeDataString(key)}",
            cancellationToken);

        await EnsureSuccessAsync(response, cancellationToken);

        return await response.Content.ReadFromJsonAsync<SettingDetailResponse>(cancellationToken)
            ?? throw new InvalidOperationException("No fue posible obtener la configuración.");
    }

    public async Task<UpdateSettingResponse> UpdateAsync(string key, UpdateSettingRequest request, CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.PutAsJsonAsync(
            $"api/settings/{Uri.EscapeDataString(key)}",
            request,
            cancellationToken);

        await EnsureSuccessAsync(response, cancellationToken);

        return await response.Content.ReadFromJsonAsync<UpdateSettingResponse>(cancellationToken)
            ?? throw new InvalidOperationException("No fue posible obtener la configuración actualizada.");
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
            return;

        var message = response.StatusCode switch
        {
            HttpStatusCode.BadRequest => "El valor enviado no es válido.",
            HttpStatusCode.Unauthorized => "Tu sesión terminó. Inicia sesión nuevamente.",
            HttpStatusCode.Forbidden => "No tienes permiso para administrar la configuración.",
            HttpStatusCode.NotFound => "La configuración ya no existe.",
            HttpStatusCode.Conflict => "La configuración no puede actualizarse en su estado actual.",
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
            // Keep the status-specific fallback for non-JSON responses.
        }

        throw new SettingsApiException(response.StatusCode, message);
    }
}

public sealed class SettingsApiException : HttpRequestException
{
    public SettingsApiException(HttpStatusCode statusCode, string message)
        : base(message, null, statusCode)
    {
    }
}
