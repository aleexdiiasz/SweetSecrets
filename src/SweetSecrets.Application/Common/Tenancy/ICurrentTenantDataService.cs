namespace SweetSecrets.Application.Common.Tenancy;

public interface ICurrentTenantDataService
{
    Task<CurrentTenantDataSummary> GetSummaryAsync(CancellationToken cancellationToken = default);
}