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
}
