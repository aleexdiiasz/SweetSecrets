namespace SweetSecrets.Application.Common.Settings;

public sealed record SettingDetail(
    string Key,
    string Value,
    string? Description,
    DateTime CreatedAt,
    DateTime? UpdatedAt);