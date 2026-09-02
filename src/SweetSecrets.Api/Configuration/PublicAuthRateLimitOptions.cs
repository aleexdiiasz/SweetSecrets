namespace SweetSecrets.Api.Configuration;

public sealed class PublicAuthRateLimitOptions
{
    public const string SectionName = "RateLimiting:PublicAuth";
    public RateLimitRuleOptions Login { get; set; } = new(10, 5);
    public RateLimitRuleOptions Register { get; set; } = new(3, 60);
    public RateLimitRuleOptions EmailDelivery { get; set; } = new(5, 15);
    public RateLimitRuleOptions TokenValidation { get; set; } = new(10, 15);
}

public sealed class RateLimitRuleOptions
{
    public RateLimitRuleOptions() { }
    public RateLimitRuleOptions(int permitLimit, int windowMinutes) { PermitLimit = permitLimit; WindowMinutes = windowMinutes; }
    public int PermitLimit { get; set; }
    public int WindowMinutes { get; set; }
}

public static class PublicAuthRateLimitPolicies
{
    public const string Login = "public-auth-login";
    public const string Register = "public-auth-register";
    public const string EmailDelivery = "public-auth-email-delivery";
    public const string TokenValidation = "public-auth-token-validation";
}
