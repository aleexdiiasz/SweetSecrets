namespace SweetSecrets.Application.Common.Settings;

public sealed record SettingListItem(
    string Key,
    string Value,
    string? Description,
    DateTime CreatedAt,
    DateTime? UpdatedAt);