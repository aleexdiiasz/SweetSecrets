using System.Text.Json;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace SweetSecrets.Api.Health;

public static class HealthCheckEndpointExtensions
{
    public static IApplicationBuilder UseOperationalHealthChecks(
        this IApplicationBuilder app)
    {
        app.UseHealthChecks("/health/live", new HealthCheckOptions
        {
            Predicate = IsLivenessCheck,
            ResponseWriter = WriteMinimalResponseAsync
        });

        app.UseHealthChecks("/health/ready", new HealthCheckOptions
        {
            Predicate = IsReadinessCheck,
            ResponseWriter = WriteMinimalResponseAsync
        });

        return app;
    }

    public static bool IsLivenessCheck(HealthCheckRegistration registration) =>
        false;

    public static bool IsReadinessCheck(HealthCheckRegistration registration) =>
        registration.Tags.Contains("ready");

    public static Task WriteMinimalResponseAsync(
        HttpContext context,
        HealthReport report)
    {
        context.Response.ContentType = "application/json";

        return context.Response.WriteAsync(JsonSerializer.Serialize(new
        {
            status = report.Status.ToString()
        }));
    }
}
