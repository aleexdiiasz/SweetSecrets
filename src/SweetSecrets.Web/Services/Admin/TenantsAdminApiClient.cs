using System.Net;
using System.Net.Http.Json;
using SweetSecrets.Contracts.Admin.Tenants;

namespace SweetSecrets.Web.Services.Admin;

public sealed class TenantsAdminApiClient(HttpClient httpClient)
{
    public async Task<TenantPageResponse> SearchAsync(string? search, string? status, CancellationToken cancellationToken = default)
    {
        var query = new List<string> { "page=1", "pageSize=50" };
        if (!string.IsNullOrWhiteSpace(search)) query.Add($"search={Uri.EscapeDataString(search)}");
        if (!string.IsNullOrWhiteSpace(status)) query.Add($"status={Uri.EscapeDataString(status)}");
        return await httpClient.GetFromJsonAsync<TenantPageResponse>($"api/admin/tenants?{string.Join('&', query)}", cancellationToken)
            ?? throw new InvalidOperationException("No fue posible obtener los tenants.");
    }

    public async Task<TenantDetailResponse?> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.GetAsync($"api/admin/tenants/{id}", cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound) return null;
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<TenantDetailResponse>(cancellationToken);
    }

    public async Task ChangeStatusAsync(Guid id, bool activate, CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.PostAsync($"api/admin/tenants/{id}/{(activate ? "activate" : "suspend")}", null, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var message = response.StatusCode == HttpStatusCode.Conflict
                ? "El estado cambió o la transición ya no está permitida."
                : "No fue posible actualizar el tenant.";
            throw new HttpRequestException(message, null, response.StatusCode);
        }
    }
}
