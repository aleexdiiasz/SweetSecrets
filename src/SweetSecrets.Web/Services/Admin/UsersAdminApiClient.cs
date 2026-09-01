using System.Net;
using System.Net.Http.Json;
using SweetSecrets.Contracts.Admin.Users;

namespace SweetSecrets.Web.Services.Admin;

public sealed class UsersAdminApiClient(HttpClient httpClient)
{
    public Task<PlatformUserPageResponse> SearchUsersAsync(string? search, string? role, bool? blocked, bool? online, CancellationToken ct = default) =>
        GetAsync<PlatformUserPageResponse>("api/admin/users", search, role, blocked, online, ct);

    public async Task<PlatformUserDetailResponse?> GetUserAsync(Guid id, CancellationToken ct = default)
    {
        using var response = await httpClient.GetAsync($"api/admin/users/{id}", ct);
        if (response.StatusCode == HttpStatusCode.NotFound) return null;
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<PlatformUserDetailResponse>(ct);
    }

    public Task<PlatformSessionPageResponse> SearchSessionsAsync(string? search, bool? active, CancellationToken ct = default) =>
        GetAsync<PlatformSessionPageResponse>("api/admin/users/sessions", search, null, null, active, ct);

    public Task SetBlockedAsync(Guid id, bool block, CancellationToken ct = default) =>
        PostAsync($"api/admin/users/{id}/{(block ? "block" : "unblock")}", "No fue posible actualizar el usuario.", ct);

    public Task RevokeSessionAsync(Guid id, CancellationToken ct = default) =>
        PostAsync($"api/admin/users/sessions/{id}/revoke", "No fue posible revocar la sesión.", ct);

    private async Task<T> GetAsync<T>(string path, string? search, string? role, bool? blocked, bool? online, CancellationToken ct)
    {
        var query = new List<string> { "page=1", "pageSize=50" };
        if (!string.IsNullOrWhiteSpace(search)) query.Add($"search={Uri.EscapeDataString(search)}");
        if (!string.IsNullOrWhiteSpace(role)) query.Add($"role={Uri.EscapeDataString(role)}");
        if (blocked.HasValue) query.Add($"isBlocked={blocked.Value.ToString().ToLowerInvariant()}");
        if (online.HasValue) query.Add($"is{(path.EndsWith("sessions") ? "Active" : "Online")}={online.Value.ToString().ToLowerInvariant()}");
        return await httpClient.GetFromJsonAsync<T>($"{path}?{string.Join('&', query)}", ct)
            ?? throw new HttpRequestException("La respuesta administrativa no fue válida.");
    }

    private async Task PostAsync(string path, string fallback, CancellationToken ct)
    {
        using var response = await httpClient.PostAsync(path, null, ct);
        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException(response.StatusCode == HttpStatusCode.Conflict
                ? "La acción no está permitida o el estado cambió." : fallback, null, response.StatusCode);
    }
}
