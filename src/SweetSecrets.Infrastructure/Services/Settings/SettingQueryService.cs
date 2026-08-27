using Microsoft.EntityFrameworkCore;
using SweetSecrets.Application.Common.Settings;
using SweetSecrets.Infrastructure.Data.Tenant;

namespace SweetSecrets.Infrastructure.Services.Settings;

public sealed class SettingQueryService : ISettingQueryService
{
    private readonly ITenantDbContextFactory _dbContextFactory;

    public SettingQueryService(ITenantDbContextFactory dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    public async Task<IReadOnlyList<SettingListItem>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        await using var dbContext =
            await _dbContextFactory.CreateAsync(
                cancellationToken);

        return await dbContext.Settings
            .AsNoTracking()
            .OrderBy(x => x.Key)
            .Select(x => new SettingListItem(
                x.Key,
                x.Value,
                x.Description,
                x.CreatedAt,
                x.UpdatedAt))
            .ToListAsync(cancellationToken);
    }

    public async Task<SettingDetail?> GetByKeyAsync(string key,
        CancellationToken cancellationToken = default)
    {
        await using var dbContext =
            await _dbContextFactory.CreateAsync(
                cancellationToken);

        return await dbContext.Settings
            .AsNoTracking()
            .Where(x => x.Key == key)
            .Select(x => new SettingDetail(
                x.Key,
                x.Value,
                x.Description,
                x.CreatedAt,
                x.UpdatedAt))
            .FirstOrDefaultAsync(cancellationToken);
    }
}