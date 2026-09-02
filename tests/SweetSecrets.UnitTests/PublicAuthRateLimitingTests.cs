using System.Reflection;
using System.Net;
using System.Text.Json;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SweetSecrets.Api.Configuration;
using SweetSecrets.Api.Controllers;
using SweetSecrets.Api.Controllers.Admin;

namespace SweetSecrets.UnitTests;

public sealed class PublicAuthRateLimitingTests
{
    [Theory]
    [InlineData(nameof(AuthController.Login),PublicAuthRateLimitPolicies.Login)]
    [InlineData(nameof(AuthController.Register),PublicAuthRateLimitPolicies.Register)]
    [InlineData(nameof(AuthController.ForgotPassword),PublicAuthRateLimitPolicies.EmailDelivery)]
    [InlineData(nameof(AuthController.ResendConfirmation),PublicAuthRateLimitPolicies.EmailDelivery)]
    [InlineData(nameof(AuthController.ResetPassword),PublicAuthRateLimitPolicies.TokenValidation)]
    [InlineData(nameof(AuthController.ConfirmEmail),PublicAuthRateLimitPolicies.TokenValidation)]
    public void SensitivePublicEndpoint_UsesExpectedPolicy(string method,string policy)
    {
        var attribute=Assert.Single(typeof(AuthController).GetMethod(method)!.GetCustomAttributes<EnableRateLimitingAttribute>());
        Assert.Equal(policy,attribute.PolicyName);
    }

    [Theory]
    [InlineData(10)]
    [InlineData(5)]
    [InlineData(3)]
    public void RequestsWithinLimitSucceed_AndNextRequestIsRejected(int permitLimit)
    {
        using var limiter=new FixedWindowRateLimiter(PublicAuthRateLimitingExtensions.CreateLimiterOptions(new RateLimitRuleOptions(permitLimit,5)));
        for(var i=0;i<permitLimit;i++){using var lease=limiter.AttemptAcquire();Assert.True(lease.IsAcquired);}
        using var rejected=limiter.AttemptAcquire();Assert.False(rejected.IsAcquired);
    }

    [Theory]
    [InlineData(PublicAuthRateLimitPolicies.Login,2)]
    [InlineData(PublicAuthRateLimitPolicies.Register,1)]
    [InlineData(PublicAuthRateLimitPolicies.EmailDelivery,2)]
    [InlineData(PublicAuthRateLimitPolicies.TokenValidation,2)]
    public async Task EndpointPolicy_AllowsNormalTraffic_ThenReturns429(string policy,int permitLimit)
    {
        await using var provider=CreateProvider(policy,permitLimit);
        var builder=new ApplicationBuilder(provider);builder.UseRateLimiter();builder.Run(context=>{context.Response.StatusCode=200;return Task.CompletedTask;});
        var application=builder.Build();
        for(var i=0;i<permitLimit;i++)Assert.Equal(200,await InvokeAsync(application,provider,policy));
        Assert.Equal(429,await InvokeAsync(application,provider,policy));
    }

    [Fact]
    public async Task Rejection_IsSmallSpanish429_AndDoesNotEnumerateAccount()
    {
        var context=new DefaultHttpContext();context.Response.Body=new MemoryStream();
        await PublicAuthRateLimitingExtensions.WriteTooManyRequestsResponseAsync(context,TimeSpan.FromMinutes(1),default);
        context.Response.Body.Position=0;var body=await new StreamReader(context.Response.Body).ReadToEndAsync();
        Assert.Equal(StatusCodes.Status429TooManyRequests,context.Response.StatusCode);
        Assert.Equal("60",context.Response.Headers.RetryAfter);
        Assert.Equal(PublicAuthRateLimitingExtensions.TooManyRequestsMessage,JsonDocument.Parse(body).RootElement.GetProperty("message").GetString());
        Assert.DoesNotContain("correo",body,StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("usuario",body,StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void AdministrativeEndpoints_AreNotAccidentallyAssignedPublicPolicies()
    {
        Assert.Empty(typeof(UsersController).GetCustomAttributes<EnableRateLimitingAttribute>());
        Assert.Empty(typeof(TenantsController).GetCustomAttributes<EnableRateLimitingAttribute>());
        Assert.Empty(typeof(AuditController).GetCustomAttributes<EnableRateLimitingAttribute>());
    }

    private static ServiceProvider CreateProvider(string policy,int limit)
    {
        var key=policy switch{PublicAuthRateLimitPolicies.Login=>"Login",PublicAuthRateLimitPolicies.Register=>"Register",PublicAuthRateLimitPolicies.EmailDelivery=>"EmailDelivery",_=>"TokenValidation"};
        var configuration=new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string?>
        {[$"RateLimiting:PublicAuth:{key}:PermitLimit"]=limit.ToString(),[$"RateLimiting:PublicAuth:{key}:WindowMinutes"]="5"}).Build();
        var services=new ServiceCollection();services.AddLogging();services.AddPublicAuthRateLimiting(configuration);return services.BuildServiceProvider();
    }

    private static async Task<int> InvokeAsync(RequestDelegate application,IServiceProvider services,string policy)
    {
        var context=new DefaultHttpContext{RequestServices=services};context.Connection.RemoteIpAddress=IPAddress.Loopback;
        context.Response.Body=new MemoryStream();context.SetEndpoint(new Endpoint(_=>Task.CompletedTask,new EndpointMetadataCollection(new EnableRateLimitingAttribute(policy)),policy));
        await application(context);return context.Response.StatusCode;
    }

}
