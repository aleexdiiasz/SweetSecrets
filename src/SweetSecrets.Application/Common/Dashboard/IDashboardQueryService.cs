namespace SweetSecrets.Application.Common.Dashboard;

public interface IDashboardQueryService
{
    Task<DashboardSummary> GetAsync(
        CancellationToken cancellationToken = default);
}
