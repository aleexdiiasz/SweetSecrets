namespace SweetSecrets.Application.Common.Settings;

public interface ISettingQueryService
{
    Task<IReadOnlyList<SettingListItem>> GetAllAsync(
        CancellationToken cancellationToken = default);

    Task<SettingDetail?> GetByKeyAsync(
        string key,
        CancellationToken cancellationToken = default);
}