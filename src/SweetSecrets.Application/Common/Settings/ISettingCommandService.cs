namespace SweetSecrets.Application.Common.Settings;

public interface ISettingCommandService
{
    Task<SettingDetail?> UpdateAsync(
        UpdateSettingCommand command,
        CancellationToken cancellationToken = default);
}