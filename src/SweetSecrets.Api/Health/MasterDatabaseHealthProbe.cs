using SweetSecrets.Infrastructure.Data.Master;

namespace SweetSecrets.Api.Health;

public sealed class MasterDatabaseHealthProbe : IMasterDatabaseHealthProbe
{
    private readonly IServiceScopeFactory _scopeFactory;

    public MasterDatabaseHealthProbe(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    public async Task<bool> CanConnectAsync(
        CancellationToken cancellationToken = default)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var dbContext = scope.ServiceProvider
            .GetRequiredService<MasterDbContext>();

        return await dbContext.Database.CanConnectAsync(cancellationToken);
    }
}
