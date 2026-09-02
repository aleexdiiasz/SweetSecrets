using SweetSecrets.Infrastructure.Services.Email;

namespace SweetSecrets.Api.Configuration;

public static class ProductionConfigurationValidator
{
    public static void Validate(
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        if (!environment.IsProduction())
        {
            return;
        }

        RequireValue(
            configuration.GetConnectionString("MasterDatabase"),
            "ConnectionStrings:MasterDatabase");

        var origins = configuration
            .GetSection("Cors:AllowedOrigins")
            .Get<string[]>() ?? [];

        if (origins.Length == 0 || origins.Any(origin =>
                !Uri.TryCreate(origin, UriKind.Absolute, out var uri) ||
                uri.Scheme != Uri.UriSchemeHttps))
        {
            throw new InvalidOperationException(
                "Cors:AllowedOrigins debe contener al menos un origen HTTPS válido en Production.");
        }

        RequireHttpsUrl(
            configuration["PasswordRecovery:ResetPageBaseUrl"],
            "PasswordRecovery:ResetPageBaseUrl");

        RequireHttpsUrl(
            configuration["EmailConfirmation:ConfirmationPageBaseUrl"],
            "EmailConfirmation:ConfirmationPageBaseUrl");

        RequireValue(configuration["Email:Smtp:Host"], "Email:Smtp:Host");
        if (!int.TryParse(configuration["Email:Smtp:Port"], out var smtpPort) || smtpPort is < 1 or > 65535)
            throw new InvalidOperationException("La configuración requerida 'Email:Smtp:Port' debe estar entre 1 y 65535 en Production.");
        RequireEmail(configuration["Email:Smtp:FromEmail"], "Email:Smtp:FromEmail");
        RequireValue(configuration["Email:Smtp:FromName"], "Email:Smtp:FromName");
        var username = configuration["Email:Smtp:Username"];
        var password = configuration["Email:Smtp:Password"];
        if (string.IsNullOrWhiteSpace(username) != string.IsNullOrWhiteSpace(password))
            throw new InvalidOperationException("Email:Smtp:Username y Email:Smtp:Password deben configurarse juntos en Production.");
    }

    private static void RequireValue(string? value, string key)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException(
                $"La configuración requerida '{key}' no está definida para Production.");
        }
    }

    private static void RequireHttpsUrl(string? value, string key)
    {
        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri) ||
            uri.Scheme != Uri.UriSchemeHttps)
        {
            throw new InvalidOperationException(
                $"La configuración requerida '{key}' debe ser una URL HTTPS absoluta en Production.");
        }
    }

    private static void RequireEmail(string? value, string key)
    {
        if (!SmtpOptionsValidator.IsValidEmail(value))
            throw new InvalidOperationException($"La configuración requerida '{key}' debe ser un correo válido en Production.");
    }
}
