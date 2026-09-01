using System.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using SweetSecrets.Api.Configuration;
using SweetSecrets.Api.Health;

namespace SweetSecrets.UnitTests;

public sealed class HealthCheckTests
{
    [Fact]
    public async Task MasterCheck_IsHealthyWhenMasterIsAvailable()
    {
        var check = new MasterDatabaseHealthCheck(
            new StubProbe(true),
            NullLogger<MasterDatabaseHealthCheck>.Instance);

        var result = await check.CheckHealthAsync(new HealthCheckContext());

        Assert.Equal(HealthStatus.Healthy, result.Status);
    }

    [Fact]
    public void Liveness_ExcludesMasterAndEveryRegisteredDependency()
    {
        var registration = new HealthCheckRegistration(
            "master_database",
            new NeverExecutedHealthCheck(),
            HealthStatus.Unhealthy,
            ["ready"]);

        Assert.False(HealthCheckEndpointExtensions.IsLivenessCheck(registration));
        Assert.True(HealthCheckEndpointExtensions.IsReadinessCheck(registration));
    }

    [Fact]
    public async Task MasterCheck_IsUnhealthyWithoutExposingFailureDetails()
    {
        var check = new MasterDatabaseHealthCheck(
            new ThrowingProbe(),
            NullLogger<MasterDatabaseHealthCheck>.Instance);

        var result = await check.CheckHealthAsync(new HealthCheckContext());

        Assert.Equal(HealthStatus.Unhealthy, result.Status);
        Assert.Null(result.Exception);
        Assert.Null(result.Description);
    }

    [Fact]
    public async Task HealthResponse_ContainsOnlyStatus()
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        var report = new HealthReport(
            new Dictionary<string, HealthReportEntry>
            {
                ["master_database"] = new(
                    HealthStatus.Unhealthy,
                    "server=secret",
                    TimeSpan.Zero,
                    new InvalidOperationException("password=secret"),
                    new Dictionary<string, object> { ["connectionString"] = "secret" })
            },
            TimeSpan.Zero);

        await HealthCheckEndpointExtensions.WriteMinimalResponseAsync(context, report);
        context.Response.Body.Position = 0;
        var body = await new StreamReader(context.Response.Body, Encoding.UTF8).ReadToEndAsync();

        Assert.Equal("{\"status\":\"Unhealthy\"}", body);
        Assert.DoesNotContain("secret", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task HealthPipeline_BypassesDatabaseDependentMiddleware()
    {
        var masterChecks = 0;
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddHealthChecks().AddCheck(
            "master_database",
            () =>
            {
                masterChecks++;
                return HealthCheckResult.Unhealthy();
            },
            tags: ["ready"]);

        await using var provider = services.BuildServiceProvider();
        var pipeline = new ApplicationBuilder(provider);
        pipeline.UseOperationalHealthChecks();
        pipeline.Run(_ => throw new InvalidOperationException(
            "Authentication and database middleware must be bypassed."));
        var application = pipeline.Build();

        var live = await InvokeAsync(application, provider, "/health/live");
        Assert.Equal(StatusCodes.Status200OK, live.StatusCode);
        Assert.Equal("{\"status\":\"Healthy\"}", live.Body);
        Assert.Equal(0, masterChecks);

        var ready = await InvokeAsync(application, provider, "/health/ready");
        Assert.Equal(StatusCodes.Status503ServiceUnavailable, ready.StatusCode);
        Assert.Equal("{\"status\":\"Unhealthy\"}", ready.Body);
        Assert.Equal(1, masterChecks);
    }

    [Fact]
    public void ProductionValidation_AcceptsCompleteHttpsConfiguration()
    {
        var configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["ConnectionStrings:MasterDatabase"] = "Host=db;Database=master",
            ["Cors:AllowedOrigins:0"] = "https://app.example.com",
            ["PasswordRecovery:ResetPageBaseUrl"] = "https://app.example.com/reset-password",
            ["EmailConfirmation:ConfirmationPageBaseUrl"] = "https://app.example.com/confirm-email"
        });

        ProductionConfigurationValidator.Validate(configuration, new TestEnvironment(Environments.Production));
    }

    [Fact]
    public void ProductionValidation_RejectsMissingCriticalConfiguration()
    {
        var exception = Assert.Throws<InvalidOperationException>(() =>
            ProductionConfigurationValidator.Validate(
                BuildConfiguration(new Dictionary<string, string?>()),
                new TestEnvironment(Environments.Production)));

        Assert.Contains("ConnectionStrings:MasterDatabase", exception.Message);
    }

    [Fact]
    public void Development_DoesNotRequireProductionConfiguration()
    {
        ProductionConfigurationValidator.Validate(
            BuildConfiguration(new Dictionary<string, string?>()),
            new TestEnvironment(Environments.Development));
    }

    private static IConfiguration BuildConfiguration(
        IDictionary<string, string?> values) =>
        new ConfigurationBuilder().AddInMemoryCollection(values).Build();

    private static async Task<(int StatusCode, string Body)> InvokeAsync(
        RequestDelegate application,
        IServiceProvider services,
        string path)
    {
        var context = new DefaultHttpContext
        {
            RequestServices = services
        };
        context.Request.Path = path;
        context.Response.Body = new MemoryStream();

        await application(context);
        context.Response.Body.Position = 0;
        var body = await new StreamReader(context.Response.Body).ReadToEndAsync();
        return (context.Response.StatusCode, body);
    }

    private sealed class StubProbe(bool result) : IMasterDatabaseHealthProbe
    {
        public Task<bool> CanConnectAsync(CancellationToken cancellationToken = default) => Task.FromResult(result);
    }

    private sealed class ThrowingProbe : IMasterDatabaseHealthProbe
    {
        public Task<bool> CanConnectAsync(CancellationToken cancellationToken = default) => throw new InvalidOperationException("sensitive");
    }

    private sealed class NeverExecutedHealthCheck : IHealthCheck
    {
        public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("Liveness must not execute registered checks.");
    }

    private sealed class TestEnvironment(string environmentName) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = environmentName;
        public string ApplicationName { get; set; } = "SweetSecrets.UnitTests";
        public string ContentRootPath { get; set; } = Directory.GetCurrentDirectory();
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
