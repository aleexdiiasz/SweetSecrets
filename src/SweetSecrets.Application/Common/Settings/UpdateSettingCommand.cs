namespace SweetSecrets.Application.Common.Settings;

public sealed record UpdateSettingCommand(
    string Key,
    string Value);