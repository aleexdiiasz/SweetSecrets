using System.Net.Http.Json;
using SweetSecrets.Contracts.Admin.Dashboard;

namespace SweetSecrets.Web.Services.Admin;

public sealed class PlatformDashboardApiClient(HttpClient httpClient)
{
    public async Task<PlatformDashboardResponse> GetAsync(CancellationToken cancellationToken = default) =>
        await httpClient.GetFromJsonAsync<PlatformDashboardResponse>("api/admin/dashboard", cancellationToken)
        ?? throw new HttpRequestException("La respuesta del dashboard no fue válida.");
}
