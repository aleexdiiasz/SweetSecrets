using System.Globalization;
using Microsoft.EntityFrameworkCore;
using SweetSecrets.Application.Common.Settings;
using SweetSecrets.Infrastructure.Data.Tenant;

namespace SweetSecrets.Infrastructure.Services.Settings;

public sealed class SettingCommandService : ISettingCommandService
{
    private readonly ITenantDbContextFactory _dbContextFactory;

    public SettingCommandService(ITenantDbContextFactory dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    public async Task<SettingDetail?> UpdateAsync(UpdateSettingCommand command, CancellationToken cancellationToken = default)
    {
        Validate(command);

        var normalizedKey = command.Key.Trim().ToUpperInvariant();
        var normalizedValue = command.Value.Trim();

        ValidateValue(
            normalizedKey,
            normalizedValue);

        await using var dbContext =
            await _dbContextFactory.CreateAsync(
                cancellationToken);

        var setting =
            await dbContext.Settings
                .FirstOrDefaultAsync(
                    x => x.Key == normalizedKey,
                    cancellationToken);

        if (setting is null)
        {
            return null;
        }

        setting.Value = NormalizeValue(
            normalizedKey,
            normalizedValue);

        setting.UpdatedAt = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);

        return new SettingDetail(
            setting.Key,
            setting.Value,
            setting.Description,
            setting.CreatedAt,
            setting.UpdatedAt);
    }

    private static void Validate(UpdateSettingCommand command)
    {
        if (string.IsNullOrWhiteSpace(command.Key))
        {
            throw new ArgumentException(
                "La clave de configuración es obligatoria.");
        }

        if (command.Key.Trim().Length > 100)
        {
            throw new ArgumentException(
                "La clave de configuración no puede exceder 100 caracteres.");
        }

        if (string.IsNullOrWhiteSpace(command.Value))
        {
            throw new ArgumentException(
                "El valor de configuración es obligatorio.");
        }

        if (command.Value.Trim().Length > 1000)
        {
            throw new ArgumentException(
                "El valor de configuración no puede exceder 1000 caracteres.");
        }
    }

    private static void ValidateValue(string key, string value)
    {
        switch (key)
        {
            case "MULTIPLIER":
                if (!decimal.TryParse(
                        value,
                        NumberStyles.AllowDecimalPoint,
                        CultureInfo.InvariantCulture,
                        out var multiplier) ||
                    multiplier <= 0)
                {
                    throw new ArgumentException(
                        "MULTIPLIER debe ser un número mayor que cero.");
                }

                break;
        }
    }

    private static string NormalizeValue(string key, string value)
    {
        return key switch
        {
            "MULTIPLIER" =>
                decimal.Parse(
                        value,
                        NumberStyles.AllowDecimalPoint,
                        CultureInfo.InvariantCulture)
                    .ToString(
                        CultureInfo.InvariantCulture),

            _ => value
        };
    }
}