using Microsoft.EntityFrameworkCore;
using SweetSecrets.Application.Common.Units;
using SweetSecrets.Infrastructure.Data.Tenant;

namespace SweetSecrets.Infrastructure.Services.Units;

public sealed class UnitQueryService : IUnitQueryService
{
    private readonly ITenantDbContextFactory _dbContextFactory;

    public UnitQueryService(
        ITenantDbContextFactory dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    public async Task<IReadOnlyList<UnitListItem>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        await using var dbContext =
            await _dbContextFactory.CreateAsync(
                cancellationToken);

        return await dbContext.Units
            .AsNoTracking()
            .OrderBy(x => x.Name)
            .Select(x => new UnitListItem(
                x.Id,
                x.Code,
                x.Name,
                x.Symbol,
                x.MeasurementType,
                x.ConversionFactor,
                x.IsActive))
            .ToListAsync(cancellationToken);
    }
}