namespace SweetSecrets.Api.Health;

public interface IMasterDatabaseHealthProbe
{
    Task<bool> CanConnectAsync(
        CancellationToken cancellationToken = default);
}
