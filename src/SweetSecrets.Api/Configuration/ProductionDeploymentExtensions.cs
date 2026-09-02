using System.Net;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;

namespace SweetSecrets.Api.Configuration;

public static class ProductionDeploymentExtensions
{
    public static IServiceCollection AddProductionDataProtection(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        if (!environment.IsProduction())
        {
            return services;
        }

        var keysPath = configuration["DataProtection:KeysPath"]!;
        var applicationName = configuration["DataProtection:ApplicationName"]!;

        services
            .AddDataProtection()
            .SetApplicationName(applicationName)
            .PersistKeysToFileSystem(new DirectoryInfo(keysPath));

        return services;
    }

    public static IServiceCollection AddTrustedForwardedHeaders(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders =
                ForwardedHeaders.XForwardedFor |
                ForwardedHeaders.XForwardedProto |
                ForwardedHeaders.XForwardedHost;
            options.RequireHeaderSymmetry = true;
            options.ForwardLimit = configuration.GetValue<int?>("ForwardedHeaders:ForwardLimit") ?? 1;

            foreach (var proxy in configuration
                         .GetSection("ForwardedHeaders:KnownProxies")
                         .Get<string[]>() ?? [])
            {
                options.KnownProxies.Add(IPAddress.Parse(proxy));
            }

            foreach (var network in configuration
                         .GetSection("ForwardedHeaders:KnownNetworks")
                         .Get<string[]>() ?? [])
            {
                options.KnownIPNetworks.Add(System.Net.IPNetwork.Parse(network));
            }
        });

        return services;
    }
}
