using System.Net.Http.Json;
using SweetSecrets.Contracts.Dashboard;

namespace SweetSecrets.Web.Services.Dashboard;

public sealed class DashboardApiClient
{
    private readonly HttpClient _httpClient;

    public DashboardApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<DashboardResponse> GetAsync(
        CancellationToken cancellationToken = default)
    {
        var dashboard = await _httpClient.GetFromJsonAsync<DashboardResponse>(
            "api/dashboard",
            cancellationToken);

        return dashboard
            ?? throw new InvalidOperationException(
                "No fue posible obtener el resumen del negocio.");
    }
}
