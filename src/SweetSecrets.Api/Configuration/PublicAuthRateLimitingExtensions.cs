using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;

namespace SweetSecrets.Api.Configuration;

public static class PublicAuthRateLimitingExtensions
{
    public const string TooManyRequestsMessage = "Has realizado demasiados intentos. Intenta nuevamente en unos minutos.";

    public static IServiceCollection AddPublicAuthRateLimiting(this IServiceCollection services, IConfiguration configuration)
    {
        var configured = configuration.GetSection(PublicAuthRateLimitOptions.SectionName).Get<PublicAuthRateLimitOptions>() ?? new();
        services.AddSingleton(Options.Create(configured));
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.OnRejected = WriteRejectedResponseAsync;
            AddPolicy(options, PublicAuthRateLimitPolicies.Login, configured.Login);
            AddPolicy(options, PublicAuthRateLimitPolicies.Register, configured.Register);
            AddPolicy(options, PublicAuthRateLimitPolicies.EmailDelivery, configured.EmailDelivery);
            AddPolicy(options, PublicAuthRateLimitPolicies.TokenValidation, configured.TokenValidation);
        });
        return services;
    }

    internal static FixedWindowRateLimiterOptions CreateLimiterOptions(RateLimitRuleOptions rule) => new()
    {
        PermitLimit = Math.Max(1, rule.PermitLimit),
        Window = TimeSpan.FromMinutes(Math.Max(1, rule.WindowMinutes)),
        QueueLimit = 0,
        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
        AutoReplenishment = true
    };

    internal static async ValueTask WriteRejectedResponseAsync(OnRejectedContext context, CancellationToken cancellationToken)
    {
        TimeSpan? retryAfter = context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var value) ? value : null;
        await WriteTooManyRequestsResponseAsync(context.HttpContext, retryAfter, cancellationToken);
    }

    internal static async Task WriteTooManyRequestsResponseAsync(HttpContext context, TimeSpan? retryAfter, CancellationToken cancellationToken)
    {
        context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        context.Response.ContentType = "application/json; charset=utf-8";
        if (retryAfter.HasValue)
            context.Response.Headers.RetryAfter = Math.Max(1, (int)Math.Ceiling(retryAfter.Value.TotalSeconds)).ToString();
        await context.Response.WriteAsJsonAsync(new { message = TooManyRequestsMessage }, cancellationToken);
    }

    private static void AddPolicy(RateLimiterOptions options, string name, RateLimitRuleOptions rule) =>
        options.AddPolicy(name, context => RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "unknown", _ => CreateLimiterOptions(rule)));
}
