using System.Net.Http.Json;
using SweetSecrets.Contracts.Units;

namespace SweetSecrets.Web.Services.Units;

public sealed class UnitsApiClient
{
    private readonly HttpClient _httpClient;

    public UnitsApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<IReadOnlyList<UnitListItemResponse>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var units =
            await _httpClient
                .GetFromJsonAsync<List<UnitListItemResponse>>(
                    "api/units",
                    cancellationToken);

        return units ?? [];
    }
}