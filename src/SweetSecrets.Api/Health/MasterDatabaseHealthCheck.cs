using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace SweetSecrets.Api.Health;

public sealed class MasterDatabaseHealthCheck : IHealthCheck
{
    private readonly IMasterDatabaseHealthProbe _probe;
    private readonly ILogger<MasterDatabaseHealthCheck> _logger;

    public MasterDatabaseHealthCheck(
        IMasterDatabaseHealthProbe probe,
        ILogger<MasterDatabaseHealthCheck> logger)
    {
        _probe = probe;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await _probe.CanConnectAsync(cancellationToken)
                ? HealthCheckResult.Healthy()
                : Unhealthy();
        }
        catch (Exception)
        {
            return Unhealthy();
        }
    }

    private HealthCheckResult Unhealthy()
    {
        _logger.LogWarning(
            "La verificación de disponibilidad de MASTER no fue satisfactoria.");

        return HealthCheckResult.Unhealthy();
    }
}
