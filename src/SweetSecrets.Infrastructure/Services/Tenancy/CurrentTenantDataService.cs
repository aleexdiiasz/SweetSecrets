using Microsoft.EntityFrameworkCore;
using SweetSecrets.Application.Common.Tenancy;
using SweetSecrets.Infrastructure.Data.Tenant;

namespace SweetSecrets.Infrastructure.Services.Tenancy;

public sealed class CurrentTenantDataService : ICurrentTenantDataService
{
    private readonly ITenantDbContextFactory _dbContextFactory;

    public CurrentTenantDataService(ITenantDbContextFactory dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    public async Task<CurrentTenantDataSummary> GetSummaryAsync(CancellationToken cancellationToken = default)
    {
        await using var dbContext = await _dbContextFactory.CreateAsync(cancellationToken);

        var units =
            await dbContext.Units.CountAsync(
                cancellationToken);

        var products =
            await dbContext.Products.CountAsync(
                cancellationToken);

        var recipes =
            await dbContext.Recipes.CountAsync(
                cancellationToken);

        return new CurrentTenantDataSummary(
            units,
            products,
            recipes);
    }
}