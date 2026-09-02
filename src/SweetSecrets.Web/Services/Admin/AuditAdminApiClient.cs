using System.Net;
using System.Net.Http.Json;
using SweetSecrets.Contracts.Admin.Audit;

namespace SweetSecrets.Web.Services.Admin;

public sealed class AuditAdminApiClient(HttpClient httpClient)
{
    public async Task<PlatformAuditPageResponse> SearchAsync(
        string? search, string? action, DateTime? from, DateTime? to,
        string? tenant, string? actor, string? targetUser, int page, int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var query = new List<string> { $"page={page}", $"pageSize={pageSize}" };
        Add(query, "search", search); Add(query, "action", action); Add(query, "tenant", tenant);
        Add(query, "actor", actor); Add(query, "targetUser", targetUser);
        if (from.HasValue) query.Add($"from={from.Value:yyyy-MM-dd}");
        if (to.HasValue) query.Add($"to={to.Value:yyyy-MM-dd}");
        return await httpClient.GetFromJsonAsync<PlatformAuditPageResponse>($"api/admin/audit?{string.Join('&', query)}", cancellationToken)
            ?? throw new HttpRequestException("La respuesta de auditoría no fue válida.");
    }

    public async Task<PlatformAuditDetailResponse?> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.GetAsync($"api/admin/audit/{id}", cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound) return null;
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<PlatformAuditDetailResponse>(cancellationToken);
    }

    private static void Add(ICollection<string> query, string name, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value)) query.Add($"{name}={Uri.EscapeDataString(value.Trim())}");
    }
}
