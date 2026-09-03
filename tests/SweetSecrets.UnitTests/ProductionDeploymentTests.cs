using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using SweetSecrets.Api.Configuration;

namespace SweetSecrets.UnitTests;

public sealed class ProductionDeploymentTests
{
    [Fact]
    public void ProductionLogging_DoesNotEmitEntityFrameworkCommandsAtInformation()
    {
        var repositoryRoot = FindRepositoryRoot();
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Path.Combine(repositoryRoot, "src", "SweetSecrets.Api"))
            .AddJsonFile("appsettings.json")
            .AddJsonFile("appsettings.Production.json")
            .Build();

        Assert.Equal(
            "Warning",
            configuration["Logging:LogLevel:Microsoft.EntityFrameworkCore.Database.Command"]);
    }

    [Theory]
    [InlineData("deploy/nginx.conf")]
    [InlineData("deploy/e2e/nginx.validation.conf")]
    public void WebManifest_IsServedWithItsSpecificMediaType(string relativePath)
    {
        var repositoryRoot = FindRepositoryRoot();
        var configuration = File.ReadAllText(Path.Combine(repositoryRoot, relativePath));

        Assert.Contains("location = /manifest.webmanifest", configuration, StringComparison.Ordinal);
        Assert.Contains("default_type application/manifest+json", configuration, StringComparison.Ordinal);
    }

    [Fact]
    public void ResponsiveShell_CoversTenantAndAdminWithoutMobileTableOverflow()
    {
        var repositoryRoot = FindRepositoryRoot();
        var styles = File.ReadAllText(Path.Combine(
            repositoryRoot, "src", "SweetSecrets.Web", "wwwroot", "css", "design-system.css"));

        Assert.Contains(".app-shell, .admin-shell", styles, StringComparison.Ordinal);
        Assert.Contains(".app-sidebar, .admin-sidebar", styles, StringComparison.Ordinal);
        Assert.Contains("min-width: 0 !important", styles, StringComparison.Ordinal);
    }

    [Fact]
    public void ForwardedHeaders_TrustOnlyConfiguredProxyNetwork()
    {
        var configuration = Configuration(new Dictionary<string, string?>
        {
            ["ForwardedHeaders:KnownNetworks:0"] = "172.30.0.0/24",
            ["ForwardedHeaders:ForwardLimit"] = "1"
        });
        var services = new ServiceCollection();
        services.AddTrustedForwardedHeaders(configuration);

        using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<ForwardedHeadersOptions>>().Value;

        Assert.Equal(1, options.ForwardLimit);
        Assert.True(options.RequireHeaderSymmetry);
        Assert.Contains(options.KnownIPNetworks, network => network.Contains(IPAddress.Parse("172.30.0.25")));
        Assert.DoesNotContain(options.KnownIPNetworks, network => network.Contains(IPAddress.Parse("203.0.113.10")));
    }

    [Fact]
    public async Task ForwardedHeaders_ResolveClientIpBeforeDownstreamMiddleware()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddTrustedForwardedHeaders(Configuration(new Dictionary<string, string?>
        {
            ["ForwardedHeaders:KnownNetworks:0"] = "172.30.0.0/24",
            ["ForwardedHeaders:ForwardLimit"] = "1"
        }));
        await using var provider = services.BuildServiceProvider();
        var builder = new ApplicationBuilder(provider);
        IPAddress? observedAddress = null;
        builder.UseForwardedHeaders();
        builder.Run(context =>
        {
            observedAddress = context.Connection.RemoteIpAddress;
            return Task.CompletedTask;
        });

        var context = new DefaultHttpContext { RequestServices = provider };
        context.Connection.RemoteIpAddress = IPAddress.Parse("172.30.0.8");
        context.Request.Headers["X-Forwarded-For"] = "198.51.100.42";
        context.Request.Headers["X-Forwarded-Proto"] = "https";
        context.Request.Headers["X-Forwarded-Host"] = "sweetsecrets.example";

        await builder.Build()(context);

        Assert.Equal(IPAddress.Parse("198.51.100.42"), observedAddress);
        Assert.Equal("https", context.Request.Scheme);
        Assert.Equal("sweetsecrets.example", context.Request.Host.Host);
    }

    [Fact]
    public void ProductionValidation_RejectsDeploymentWithoutTrustedProxy()
    {
        var values = CompleteProductionConfiguration();
        values.Remove("ForwardedHeaders:KnownNetworks:0");

        var exception = Assert.Throws<InvalidOperationException>(() =>
            ProductionConfigurationValidator.Validate(
                Configuration(values),
                new TestEnvironment(Environments.Production)));

        Assert.Contains("ForwardedHeaders", exception.Message);
    }

    [Fact]
    public void DataProtectionKeys_SurviveServiceProviderRecreation()
    {
        var keysPath = Path.Combine(Path.GetTempPath(), "SweetSecrets", "tests", Guid.NewGuid().ToString("N"));
        try
        {
            var configuration = Configuration(new Dictionary<string, string?>
            {
                ["DataProtection:KeysPath"] = keysPath,
                ["DataProtection:ApplicationName"] = "SweetSecrets.Tests"
            });
            var environment = new TestEnvironment(Environments.Production);
            string protectedValue;

            using (var first = Provider(configuration, environment))
            {
                protectedValue = first.GetRequiredService<IDataProtectionProvider>()
                    .CreateProtector("deployment-test")
                    .Protect("persistent-value");
            }

            using var second = Provider(configuration, environment);
            var value = second.GetRequiredService<IDataProtectionProvider>()
                .CreateProtector("deployment-test")
                .Unprotect(protectedValue);

            Assert.Equal("persistent-value", value);
            Assert.NotEmpty(Directory.EnumerateFiles(keysPath, "key-*.xml"));
        }
        finally
        {
            if (Directory.Exists(keysPath))
                Directory.Delete(keysPath, recursive: true);
        }
    }

    private static ServiceProvider Provider(IConfiguration configuration, IHostEnvironment environment)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddProductionDataProtection(configuration, environment);
        return services.BuildServiceProvider();
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "SweetSecrets.slnx")))
            directory = directory.Parent;

        return directory?.FullName ?? throw new DirectoryNotFoundException("No se encontró la raíz del repositorio.");
    }

    private static IConfiguration Configuration(IDictionary<string, string?> values) =>
        new ConfigurationBuilder().AddInMemoryCollection(values).Build();

    private static Dictionary<string, string?> CompleteProductionConfiguration() => new()
    {
        ["ConnectionStrings:MasterDatabase"] = "Host=db;Database=master",
        ["Cors:AllowedOrigins:0"] = "https://app.example.com",
        ["PasswordRecovery:ResetPageBaseUrl"] = "https://app.example.com/reset-password",
        ["EmailConfirmation:ConfirmationPageBaseUrl"] = "https://app.example.com/confirm-email",
        ["Email:Smtp:Host"] = "smtp.example.com",
        ["Email:Smtp:Port"] = "587",
        ["Email:Smtp:FromEmail"] = "no-reply@example.com",
        ["Email:Smtp:FromName"] = "SweetSecrets",
        ["DataProtection:KeysPath"] = "/keys",
        ["DataProtection:ApplicationName"] = "SweetSecrets",
        ["ForwardedHeaders:KnownNetworks:0"] = "172.30.0.0/24",
        ["ForwardedHeaders:ForwardLimit"] = "1",
        ["BootstrapAdmin:Email"] = "admin@example.com",
        ["BootstrapAdmin:Password"] = "external-secret",
        ["BootstrapAdmin:FullName"] = "Platform Admin"
    };

    private sealed class TestEnvironment(string name) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = name;
        public string ApplicationName { get; set; } = "Tests";
        public string ContentRootPath { get; set; } = Directory.GetCurrentDirectory();
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
